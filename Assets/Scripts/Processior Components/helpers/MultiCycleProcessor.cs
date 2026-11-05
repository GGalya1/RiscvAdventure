/// <summary>
/// Multi-cycle (Mehrtakt) RISC-V datapath with one shared instruction/data memory
/// and the non-architectural registers OldPC, Instr, Data, SrcA, SrcB and ALUOut.
/// <br/>The control signals are set by the player through the bound views.
/// </summary>
public sealed class MultiCycleProcessor
{
    public Register Pc { get; }
    public Register OldPc { get; } = new();
    public Register Instruction { get; } = new();
    public Register Data { get; } = new();
    public Register SrcA { get; } = new();
    public Register SrcB { get; } = new();
    public Register AluOut { get; } = new();

    public DataInstMemory Memory { get; } = new(writeEnable: true);
    public RegisterFile RegisterFile { get; }

    public Alu Alu { get; } = new();
    public Extender Extender { get; } = new();

    public Multiplexer AdrMux { get; } = new();     // memory address: PC / Result
    public Multiplexer SrcAMux { get; } = new();    // ALU A: PC / OldPC / SrcA
    public Multiplexer SrcBMux { get; } = new();    // ALU B: SrcB / ImmExt / 4
    public Multiplexer ResultMux { get; } = new();  // Result: ALUOut / Data / ALUResult

    private readonly ClockDomain _clock;

    public MultiCycleProcessor(ClockDomain clock, int initialPc, int[] memoryWords, int[] registers)
    {
        _clock = clock;

        Pc = new Register(initialPc, writeEnable: true);
        Memory.LoadWords(memoryWords);
        RegisterFile = new RegisterFile(registers, writeEnable: true);

        clock.Add(Pc, OldPc, Instruction, Data, SrcA, SrcB, AluOut, Memory, RegisterFile);
    }

    #region Control signals
    public MultiCycleControls Controls => new(
        Alu.Operation, Extender.Type,
        AdrMux.SelectedInput, SrcAMux.SelectedInput, SrcBMux.SelectedInput, ResultMux.SelectedInput);

    private void ApplyControls(MultiCycleControls c)
    {
        Alu.Operation = c.Alu;
        Extender.Type = c.Immediate;
        AdrMux.SelectedInput = c.AdrMux;
        SrcAMux.SelectedInput = c.SrcAMux;
        SrcBMux.SelectedInput = c.SrcBMux;
        ResultMux.SelectedInput = c.ResultMux;
    }
    #endregion

    /// <summary> Immediate of the instruction currently held in the Instr register. </summary>
    public int ImmExt => Extender.Extend(Instruction.Output);

    /// <summary> All multiplexer/ALU outputs of the current state (no side effects). </summary>
    public MultiCycleSignals ComputeSignals()
    {
        var srcA = SrcAMux.Route(Pc.Output, OldPc.Output, SrcA.Output);
        var srcB = SrcBMux.Route(SrcB.Output, ImmExt, 4);
        var aluResult = Alu.Compute(srcA, srcB);
        var result = ResultMux.Route(AluOut.Output, Data.Output, aluResult);
        var address = AdrMux.Route(Pc.Output, result);

        return new MultiCycleSignals(address, srcA, srcB, aluResult, result);
    }

    /// <summary> One clock edge: drive the inputs of all sequential elements and latch them. </summary>
    public void Step()
    {
        var sig = ComputeSignals();
        var instr = Instruction.Output;

        // memory
        var word = Memory.Read(sig.Address);
        Instruction.Input = word;
        Data.Input = word;
        OldPc.Input = Pc.Output;
        Memory.Address = sig.Address;
        Memory.WriteData = SrcB.Output;

        // register file
        RegisterFile.ReadAddress1 = InstructionFields.Rs1(instr);
        RegisterFile.ReadAddress2 = InstructionFields.Rs2(instr);
        RegisterFile.ReadRegisters();
        SrcA.Input = RegisterFile.ReadData1;
        SrcB.Input = RegisterFile.ReadData2;

        // ALU
        AluOut.Input = sig.AluResult;

        // write back
        RegisterFile.WriteAddress = InstructionFields.Rd(instr);
        RegisterFile.WriteData = sig.Result;
        Pc.Input = sig.Result;

        _clock.Tick();
    }

    #region History
    public MultiCycleProcessorState Save() => new(
        Pc.Save(), OldPc.Save(), Instruction.Save(), Data.Save(), SrcA.Save(), SrcB.Save(), AluOut.Save(),
        Memory.Save(), RegisterFile.Save(), Controls);

    public void Restore(MultiCycleProcessorState s)
    {
        Pc.Restore(s.Pc);
        OldPc.Restore(s.OldPc);
        Instruction.Restore(s.Instruction);
        Data.Restore(s.Data);
        SrcA.Restore(s.SrcA);
        SrcB.Restore(s.SrcB);
        AluOut.Restore(s.AluOut);
        Memory.Restore(s.Memory);
        RegisterFile.Restore(s.RegisterFile);
        ApplyControls(s.Controls);
    }
    #endregion
}

public readonly struct MultiCycleControls
{
    public readonly AluOperation Alu;
    public readonly ImmediateType Immediate;
    public readonly int AdrMux;
    public readonly int SrcAMux;
    public readonly int SrcBMux;
    public readonly int ResultMux;

    public MultiCycleControls(AluOperation alu, ImmediateType immediate, int adrMux, int srcAMux, int srcBMux, int resultMux)
    {
        Alu = alu;
        Immediate = immediate;
        AdrMux = adrMux;
        SrcAMux = srcAMux;
        SrcBMux = srcBMux;
        ResultMux = resultMux;
    }
}

public readonly struct MultiCycleSignals
{
    public readonly int Address;     // ADR MUX output
    public readonly int SrcA;        // SrcA MUX output
    public readonly int SrcB;        // SrcB MUX output
    public readonly int AluResult;
    public readonly int Result;      // Result MUX output

    public MultiCycleSignals(int address, int srcA, int srcB, int aluResult, int result)
    {
        Address = address;
        SrcA = srcA;
        SrcB = srcB;
        AluResult = aluResult;
        Result = result;
    }
}

public readonly struct MultiCycleProcessorState
{
    public readonly RegisterState Pc, OldPc, Instruction, Data, SrcA, SrcB, AluOut;
    public readonly MemoryState Memory;
    public readonly RegisterFileState RegisterFile;
    public readonly MultiCycleControls Controls;

    public MultiCycleProcessorState(
        RegisterState pc, RegisterState oldPc, RegisterState instruction, RegisterState data,
        RegisterState srcA, RegisterState srcB, RegisterState aluOut,
        MemoryState memory, RegisterFileState registerFile, MultiCycleControls controls)
    {
        Pc = pc;
        OldPc = oldPc;
        Instruction = instruction;
        Data = data;
        SrcA = srcA;
        SrcB = srcB;
        AluOut = aluOut;
        Memory = memory;
        RegisterFile = registerFile;
        Controls = controls;
    }
}
