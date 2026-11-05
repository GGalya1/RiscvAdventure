using System.Collections.Generic;

// Anything a word can be read from: the live memory or a saved snapshot of it
public interface IReadableMemory
{
    // Word at address, or 0 if nothing is stored there
    int Read(int address);
}

/// <summary>
/// Word-addressed instruction/data memory (A, WD, WE inputs; RD output).
/// <br/>Reading is combinational (<see cref="Read"/>), writing happens on the clock edge.
/// </summary>
public class DataInstMemory : ISequentialLogic, IReadableMemory
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

    // Loads consecutive words starting at address 0 (0, 4, 8, 12)
    public void LoadWords(params int[] words)
    {
        for (var i = 0; i < words.Length; i++)
            LoadWord(i * WordSize, words[i]);
    }

    // Removes all stored words
    public void Clear()
    {
        _words.Clear();
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
}