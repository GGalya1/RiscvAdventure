using System;
using System.Collections.Generic;
using System.Linq;

namespace RiscvAdventure.PlayModeTests
{
    /// <summary>
    /// The intended solution of every hand-built puzzle level (chapters 1-6).
    ///
    /// How to read a recipe: <c>Mux("multiplexerVisualizer", 0)</c> clicks the first input button of the
    /// multiplexer stored in the level script's field <c>multiplexerVisualizer</c>. Field names are the C# field
    /// names of the level's "Regisseur" class, so a rename in game code shows up as a clear test failure.
    /// <c>.Before(...)</c> lists what to set up before a tick; a bare <c>.Tick()</c> with no preceding
    /// <c>.Before(...)</c> means "just tick, nothing new to set up".
    ///
    /// The processor levels (Level 10 / Level 11, 12 sub-levels) are not covered here yet.
    /// </summary>
    public static class LevelRecipes
    {
        // ---- building blocks ------------------------------------------------------------

        private static Action<LevelDriver> Mux(string field, int path) => d => d.SelectMux(field, path);
        private static Action<LevelDriver> Alu(string field, Op op) => d => d.ChooseAlu(field, op);
        private static Action<LevelDriver> Immediate(string field, Imm type) => d => d.ChooseImmediate(field, type);
        private static Action<LevelDriver> WriteEnable(string field, bool on) => d => d.SetWriteEnable(field, on);

        // Field names used by several levels
        private const string MuxField = "multiplexerVisualizer";
        private const string AluField = "aluVisualizer";
        private const string ExtenderField = "extenderVisualizer";
        private const string OutputField = "registerOutputVisualizer";   // in levels 3/4 this is the data memory
        private const string SrcBField = "registerSrcBVisualizer";

        // ---- intended solutions ---------------------------------------------------------

        public static readonly IReadOnlyList<Recipe> Solutions = new List<Recipe>
        {
            // Chapter 1 - Registers. Every register is clocked each tick, so a value moves one
            // register further per tick (A -> B -> Out) and registers without an input load 0.
            Recipe.For("Level 0", "2 ticks: 4 travels A -> B -> Out")
                .Tick().Tick()
                .Build(),
            Recipe.For("Level 0-1", "1 tick: 42 (B) reaches Out; a 2nd tick would push 256 there")
                .Tick()
                .Build(),
            Recipe.For("Level 0-2", "2 ticks: -12 reaches Out on tick 2")
                .Tick().Tick()
                .Build(),

            // Chapter 2 - Multiplexers.
            Recipe.For("Level 1", "MUX -> input 0 (register A = 4), 1 tick")
                .Before(Mux(MuxField, 0)).Tick()
                .Build(),
            Recipe.For("Level 1-1", "MUX -> input 1 (register B = 2), 1 tick")
                .Before(Mux(MuxField, 1)).Tick()
                .Build(),
            Recipe.For("Level 1-2", "middle MUX = 12, lower MUX = 'middle', output MUX = 'lower' -> 12")
                .Before(Mux("middleMuxVisualizer", 1), Mux("downMuxVisualizer", 0), Mux("outputMuxVisualizer", 2)).Tick()
                .Build(),

            // Chapter 3 - ALU.
            Recipe.For("Level 2", "A = 1 -> 5 (ADD) -> 4 (AND) -> 8 (ADD)")
                .Before(Alu(AluField, Op.Add)).Tick()
                .Before(Alu(AluField, Op.And)).Tick()
                .Before(Alu(AluField, Op.Add)).Tick()
                .Build(),
            Recipe.For("Level 2-1", "43 AND 21 = 1")
                .Before(Alu(AluField, Op.And)).Tick()
                .Build(),
            Recipe.For("Level 2-2", "7 AND 7 = 7 (OR would work too)")
                .Before(Alu(AluField, Op.And)).Tick()
                .Build(),

            // Chapter 4 - Memory. Address register A, data register B.
            Recipe.For("Level 3-0", "MUX on, 1 tick: B = memory[4] = 128")
                .Before(Mux(MuxField, 0)).Tick()
                .Build(),
            Recipe.For("Level 3", "A: 5 -AND-> 4 -ADD-> 8 -ADD-> 12, then tick 4 loads memory[12] = 66")
                .Before(Mux(MuxField, 0), Alu(AluField, Op.And)).Tick()
                .Before(Alu(AluField, Op.Add)).Tick()
                .Tick()
                .Tick()
                .Build(),
            Recipe.For("Level 4-0", "put B (8) at address 12: A = B + 4 with B and memory frozen, then store")
                .Before(Mux(MuxField, 1), Alu(AluField, Op.Add), WriteEnable(SrcBField, false), WriteEnable(OutputField, false)).Tick()
                .Before(Mux(MuxField, 0), WriteEnable(SrcBField, true), WriteEnable(OutputField, true)).Tick()
                .Build(),

            // Chapter 4 - see KnownIssue: as shipped, this level compares memory[0] with memory[0].
            Recipe.For("Level 4", "load -1024 into B and store it at address 0 (memory[0] < memory[8])")
                .Before(Mux(MuxField, 1), Alu(AluField, Op.Add)).Tick()
                .Before(Alu(AluField, Op.And)).Tick()
                .Before(Mux(MuxField, 0), Alu(AluField, Op.Add)).Tick()
                .Build()
                .WithKnownIssue(
                    "Level 4.unity has correctAnswer = 0, so LevelFourthRegisseur.CheckWinCondition() evaluates " +
                    "Memory[0] < Memory[0], which can never be true. The goal text says 'addresses 0 and 8', so " +
                    "correctAnswer is probably meant to be 8 (Level 4-0 uses 12 for 'addresses 0 and 12')."),

            // Chapter 5 - Extender. The type has to be chosen before each tick.
            Recipe.For("Level 5-1", "I-type immediate: 2000")
                .Before(Immediate(ExtenderField, Imm.I)).Tick()
                .Build(),
            Recipe.For("Level 5-2", "S-type immediate: 4 (B-type works too)")
                .Before(Immediate(ExtenderField, Imm.S)).Tick()
                .Build(),
            Recipe.For("Level 5", "decode the four stored words: I/J -> 1000, S/B -> 8, S/B -> 8, I -> 4")
                .Before(Immediate(ExtenderField, Imm.I)).Tick()
                .Before(Immediate(ExtenderField, Imm.I)).Tick()
                .Before(Immediate(ExtenderField, Imm.S)).Tick()
                .Before(Immediate(ExtenderField, Imm.S)).Tick()
                .Before(Immediate(ExtenderField, Imm.I)).Tick()
                .Build(),

            // Chapter 6 - Register file. The register file is read one tick before the ALU uses the result.
            Recipe.For("Level 7", "ALU = ADD (or OR): registers 6 and 7 hold 2 and 40; Out = 42 after 2 ticks")
                .Before(Alu(AluField, Op.Add)).Tick()
                .Tick()
                .Build(),
            Recipe.For("Level 7-1", "ALU = ADD: 39 + 3 is written back to register 0 on tick 2")
                .Before(Alu(AluField, Op.Add)).Tick()
                .Tick()
                .Build(),
            Recipe.For("Level 7-2", "MUX = immediate, I-type -> 256, ALU = ADD: written back to register 0 on tick 2")
                .Before(Mux("muxVisualizer", 1), Immediate(ExtenderField, Imm.I), Alu(AluField, Op.Add)).Tick()
                .Tick()
                .Build(),
        };

        /// <summary>Scene names of every level covered by <see cref="Solutions"/>.</summary>
        public static IEnumerable<string> Scenes => Solutions.Select(r => r.Scene);
    }
}
