/// <summary>
/// Управляющие воздействия оператора.
/// Могут меняться в процессе работы симулятора.
/// </summary>
public sealed class ControlInputs
{
    public double G_izvest { get; set; } = 11000.0; // расход извести, кг/ч
    public double G_coks   { get; set; } = 4000.0;  // расход кокса, кг/ч
    public double K_ctrl   { get; set; } = 0;        // направление электрода: -1/0/+1
    public double L_ctrl   { get; set; } = 0.05;     // скорость перемещения электрода, м/ч
}
