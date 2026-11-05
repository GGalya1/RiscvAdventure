using System.Collections.Generic;

/// <summary> Anything a word can be read from: the live memory or a saved snapshot of it. </summary>
public interface IReadableMemory
{
    /// <summary> Word at <paramref name="address"/>, or 0 if nothing is stored there. </summary>
    int Read(int address);
}

/// <summary>
/// Word-addressed instruction/data memory (A, WD, WE inputs; RD output).
/// <br/>Reading is combinational (<see cref="Read"/>), writing happens on the clock edge.
/// </summary>
public sealed class DataInstMemory : ISequentialLogic, IReadableMemory
{
    public const int WordSize = 4;

    #region INPUTS
    // WE
    public bool WriteEnable { get; set; }

    // WD
    public int WriteData { get; set; }

    // A
    public int Address { get; set; }
    #endregion

    #region OUTPUTS
    // RD
    public int ReadData { get; private set; }
    #endregion

    // address -> word (instructions and data)
    private readonly Dictionary<int, int> _words = new();

    public DataInstMemory(bool writeEnable = true)
    {
        WriteEnable = writeEnable;
    }

    public void LoadWord(int address, int data)
    {
        _words[address] = data;
    }

    /// <summary> Loads consecutive words starting at address 0 (0, 4, 8, ...). </summary>
    public void LoadWords(params int[] words)
    {
        for (var i = 0; i < words.Length; i++)
            LoadWord(i * WordSize, words[i]);
    }

    public int Read(int address) => _words.TryGetValue(address, out var word) ? word : 0;

    public bool Contains(int address) => _words.ContainsKey(address);

    #region Sequential logic
    public void PreClockUpdate()
    {
        ReadData = Read(Address);
    }

    public void Clock()
    {
        if (WriteEnable)
        {
            _words[Address] = WriteData;
        }
    }
    #endregion

    public MemoryState Save() => new(new Dictionary<int, int>(_words), WriteEnable);

    public void Restore(MemoryState state)
    {
        _words.Clear();
        if (state.Words != null)
        {
            foreach (var pair in state.Words) _words[pair.Key] = pair.Value;
        }
        WriteEnable = state.WriteEnable;
    }
}

/// <summary> Snapshot of a <see cref="DataInstMemory"/> for the tick history. </summary>
public readonly struct MemoryState : IReadableMemory
{
    public readonly IReadOnlyDictionary<int, int> Words;
    public readonly bool WriteEnable;

    public MemoryState(IReadOnlyDictionary<int, int> words, bool writeEnable)
    {
        Words = words;
        WriteEnable = writeEnable;
    }

    public int Read(int address) => Words != null && Words.TryGetValue(address, out var word) ? word : 0;
}
