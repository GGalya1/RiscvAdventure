using System;
using System.Collections.Generic;

/// <summary> What the player has to achieve in a processor sub-level. Serialized as int in the level assets. </summary>
public enum ExerciseTyp
{
    REGISTER_FIELD = 0,
    MEMORY = 1,
    BEQ = 2,
    JAL = 3,
}

public static class ExerciseCheck
{
    /// <summary> Checks the processor state against the answer stored in <paramref name="level"/>. </summary>
    public static bool IsSolved(BaseProcessorInitialState level, IReadOnlyList<int> registers, Func<int, int> readMemory, int pc)
    {
        bool RegisterCorrect() => registers[level.registerFieldAddressAnswer] == level.registerFieldValueAnswer;
        bool MemoryCorrect() => readMemory(level.memoryAddressAnswer) == level.memoryValueAnswer;
        bool PcCorrect() => pc == level.pcValueAnswer;

        return level.aufgabeTyp switch
        {
            ExerciseTyp.REGISTER_FIELD => RegisterCorrect(),
            ExerciseTyp.MEMORY => MemoryCorrect(),
            ExerciseTyp.BEQ => PcCorrect(),
            ExerciseTyp.JAL => PcCorrect() && RegisterCorrect(),
            _ => false
        };
    }
}