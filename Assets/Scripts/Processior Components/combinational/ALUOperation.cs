/// <summary>
/// Control signal of the ALU. The numeric values are the button indices in the ALU panel
/// and the values stored in level snapshots, so they must not change.
/// </summary>
public enum AluOperation
{
    // Arithmetic
    ADD = 0,    // Addition (R-type: ADD, I-type: ADDI)
    SUB = 1,    // Subtraction (R-type: SUB, B-type: BEQ/BNE)

    // Logic
    AND = 2,    // Logical AND
    OR = 3,     // Logical OR
}