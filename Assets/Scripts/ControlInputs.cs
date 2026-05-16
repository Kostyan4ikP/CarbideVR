using UnityEngine;

public class ControlInputs
{
    public double G_lime { get; set; }  // расход извести, кг/ч
    public double G_coke { get; set; }  // расход кокса, кг/ч
    public double K_ctrl { get; set; }  // направление электрода: -1/0/+1
    public double L_ctrl { get; set; }  // скорость перемещения электрода, м/ч
}
