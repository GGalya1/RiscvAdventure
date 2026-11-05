public static class Alu
{
    public static int Calculate(int a, int b, AluOperation operation) => operation switch
    {
        AluOperation.ADD => a + b,
        AluOperation.SUB => a - b,
        AluOperation.AND => a & b,
        AluOperation.OR => a | b,
        _ => 0
    };
}