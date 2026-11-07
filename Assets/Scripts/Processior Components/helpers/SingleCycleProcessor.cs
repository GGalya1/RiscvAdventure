using System.Collections.Generic;

/// <summary>
/// Single-cycle (Eintakt) RISC-V datapath: separate instruction and data memory,
/// every instruction is completed in one clock edge.
/// <br/>The control signals are not decoded from the instruction: the player sets them
/// through the views bound to <see cref="Alu"/>, <see cref="Extender"/>, the multiplexers and the WE toggles.
/// </summary>
public sealed class SingleCycleProcessor
{
    public Register Pc { get; }
    public DataInstMemory InstructionMemory { get; }
    public DataInstMemory DataMemory { get; }
    public RegisterFile RegisterFile { get; }

    public Alu Alu { get; } = new();
    public Alu PcAlu { get; } = new();      // PC + 4
    public Alu BtaAlu { get; } = new();     // PC + ImmExt (branch target address)
    public Extender Extender { get; } = new();

    public Multiplexer PcMux { get; } = new();      // PCNext: PC+4 / BTA
    public Multiplexer SrcBMux { get; } = new();    // ALU SrcB: RD2 / ImmExt
    public Multiplexer ResultMux { get; } = new();  // Result: ALUResult / ReadData / PC+4

    private readonly ClockDomain _clock;

    public SingleCycleProcessor(ClockDomain clock, int initialPc, int[] instructions, int[] data, int[] registers)
    {
        _clock = clock;

        Pc = new Register(initialPc, writeEnable: true);

        InstructionMemory = new DataInstMemory(writeEnable: false);
        InstructionMemory.LoadWords(instructions);

        DataMemory = new DataInstMemory(writeEnable: false);
        DataMemory.LoadWords(data);

        RegisterFile = new RegisterFile(registers, writeEnable: false);

        // the instruction memory is read-only and therefore not clocked
        clock.Add(Pc, DataMemory, RegisterFile);
    }

    #region Control signals
    public SingleCycleControls Controls => new(
        Alu.Operation, PcAlu.Operation, BtaAlu.Operation, Extender.Type,
        SrcBMux.SelectedInput, ResultMux.SelectedInput, PcMux.SelectedInput);

    private void ApplyControls(SingleCycleControls c)
    {
        Alu.Operation = c.Alu;
        PcAlu.Operation = c.PcAlu;
        BtaAlu.Operation = c.Bta;
        Extender.Type = c.Immediate;
        SrcBMux.SelectedInput = c.SrcBMux;
        ResultMux.SelectedInput = c.ResultMux;
        PcMux.SelectedInput = c.PcMux;
    }
    #endregion

    #region Combinational logic
    /// <summary> All bus values of the current cycle (no side effects). </summary>
    public SingleCycleSignals ComputeSignals() =>
        ComputeSignals(Pc.Output, RegisterFile.Registers, DataMemory, Controls);

    /// <summary> All bus values of the cycle that started in the saved <paramref name="state"/>. </summary>
    public SingleCycleSignals ComputeSignals(SingleCycleProcessorState state) =>
        ComputeSignals(state.Pc.Value, state.RegisterFile.Values, state.DataMemory, state.Controls);

    private SingleCycleSignals ComputeSignals(
        int pc, IReadOnlyList<int> registers, IReadableMemory dataMemory, SingleCycleControls c)
    {
        // IF
        var instr = InstructionMemory.Read(pc);

        // ID
        var rs1 = InstructionFields.Rs1(instr);
        var rs2 = InstructionFields.Rs2(instr);
        var rd = InstructionFields.Rd(instr);
        var rd1 = RegisterFile.ReadPort(registers, rs1);
        var rd2 = RegisterFile.ReadPort(registers, rs2);
        var immExt = Extender.Evaluate(c.Immediate, (uint)instr);

        // EX
        var srcB = Multiplexer.Select(c.SrcBMux, rd2, immExt);
        var aluResult = Alu.Calculate(rd1, srcB, c.Alu);
        var pcPlus4 = Alu.Calculate(pc, 4, c.PcAlu);
        var bta = Alu.Calculate(pc, immExt, c.Bta);

        // MEM
        var readData = dataMemory.Read(aluResult);

        // WB
        var result = Multiplexer.Select(c.ResultMux, aluResult, readData, pcPlus4);
        var pcNext = Multiplexer.Select(c.PcMux, pcPlus4, bta);

        return new SingleCycleSignals(
            pc, instr, rs1, rs2, rd, rd1, rd2, immExt,
            srcB, aluResult, pcPlus4, bta, readData, result, pcNext);
    }
    #endregion

    /// <summary> One clock edge: drive the inputs of all sequential elements and latch them. </summary>
    public void Step()
    {
        var sig = ComputeSignals();

        RegisterFile.ReadAddress1 = sig.Rs1;
        RegisterFile.ReadAddress2 = sig.Rs2;
        RegisterFile.WriteAddress = sig.Rd;
        RegisterFile.WriteData = sig.Result;

        DataMemory.Address = sig.AluResult;
        DataMemory.WriteData = sig.Rd2;

        Pc.Input = sig.PcNext;

        _clock.Tick();
    }

    #region History
    public SingleCycleProcessorState Save() => new(
        Pc.Save(), InstructionMemory.Save(), DataMemory.Save(), RegisterFile.Save(), Controls);

    public void Restore(SingleCycleProcessorState state)
    {
        Pc.Restore(state.Pc);
        InstructionMemory.Restore(state.InstructionMemory);
        DataMemory.Restore(state.DataMemory);
        RegisterFile.Restore(state.RegisterFile);
        ApplyControls(state.Controls);
    }
    #endregion
}

public readonly struct SingleCycleControls
{
    public readonly AluOperation Alu;
    public readonly AluOperation PcAlu;
    public readonly AluOperation Bta;
    public readonly ImmediateType Immediate;
    public readonly int SrcBMux;
    public readonly int ResultMux;
    public readonly int PcMux;

    public SingleCycleControls(
        AluOperation alu, AluOperation pcAlu, AluOperation bta, ImmediateType immediate,
        int srcBMux, int resultMux, int pcMux)
    {
        Alu = alu;
        PcAlu = pcAlu;
        Bta = bta;
        Immediate = immediate;
        SrcBMux = srcBMux;
        ResultMux = resultMux;
        PcMux = pcMux;
    }
}

/// <summary> Immutable snapshot of every bus value of one single-cycle clock period. </summary>
public readonly struct SingleCycleSignals
{
    public readonly int Pc;
    public readonly int Instr;
    public readonly int Rs1, Rs2, Rd;
    public readonly int Rd1, Rd2;
    public readonly int ImmExt;
    public readonly int SrcB;
    public readonly int AluResult;
    public readonly int PcPlus4;
    public readonly int Bta;
    public readonly int ReadData;
    public readonly int Result;
    public readonly int PcNext;

    public SingleCycleSignals(
        int pc, int instr, int rs1, int rs2, int rd, int rd1, int rd2, int immExt,
        int srcB, int aluResult, int pcPlus4, int bta, int readData, int result, int pcNext)
    {
        Pc = pc;
        Instr = instr;
        Rs1 = rs1;
        Rs2 = rs2;
        Rd = rd;
        Rd1 = rd1;
        Rd2 = rd2;
        ImmExt = immExt;
        SrcB = srcB;
        AluResult = aluResult;
        PcPlus4 = pcPlus4;
        Bta = bta;
        ReadData = readData;
        Result = result;
        PcNext = pcNext;
    }
}

public readonly struct SingleCycleProcessorState
{
    public readonly RegisterState Pc;
    public readonly MemoryState InstructionMemory;
    public readonly MemoryState DataMemory;
    public readonly RegisterFileState RegisterFile;
    public readonly SingleCycleControls Controls;

    public SingleCycleProcessorState(
        RegisterState pc, MemoryState instructionMemory, MemoryState dataMemory,
        RegisterFileState registerFile, SingleCycleControls controls)
    {
        Pc = pc;
        InstructionMemory = instructionMemory;
        DataMemory = dataMemory;
        RegisterFile = registerFile;
        Controls = controls;
    }
}
