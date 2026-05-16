using Mono.Cecil;
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SimulationStep
{
    public double Time { get; set; }
    public double C { get; set; }
    public double L_mpr { get; set; }
    public double Temperature { get; set; }
    public double R { get; set; }
    public double E { get; set; }
    public double U { get; set; }
    public double Q { get; set; }
    public double W { get; set; }
    public double Mraspl { get; set; }  // текущая масса расплава, кг
    public double Gprod { get; set; }  // производительность на данном шаге, кг/ч
    public double KPD { get; set; }  // текущий КПД
}
public class MathModel
{
    // ── Геометрия печи (прямоугольная ванна) ────────────────────────────
    public double FurnaceLength { get; set; } = 9.9;   // длина ванны, м
    public double FurnaceWidth { get; set; } = 7.9;   // ширина ванны, м
    public double H { get; set; } = 4.6;   // высота печи, м
    public double D_el { get; set; } = 1.3;   // диаметр электрода, м

    // ── Физические свойства материалов ──────────────────────────────────
    public double Rho_prod { get; set; } = 2220;   // плотность расплава CaC₂, кг/м³
    public double Rho_el { get; set; } = 400;    // плотность электродной массы, кг/м³
    public double Craspl { get; set; } = 0.28;   // теплоёмкость расплава, кДж/(кг·°C)

    // ── Уровень расплава и слив ──────────────────────────────────────────
    public double H_melt_max { get; set; } = 0.30;  // макс. допустимая высота расплава, м
    public double H_melt_min { get; set; } = 0.06;  // высота остатка расплава после слива, м

    // ── Начальная концентрация и номинальный режим ───────────────────────
    public double C_CaC2_0 { get; set; } = 65;    // начальная концентрация CaC₂, %
    public double L_mpr_nom { get; set; } = 1.0;   // номинальное МПР, м
    public double T_nom { get; set; } = 2000;  // номинальная температура расплава, °C
    public double Tsmelt { get; set; } = 1417;  // температура зоны плавления, °C
    public double T0 { get; set; } = 25;    // температура окружающей среды, °C
    public double Cprod_nom { get; set; } = 75.0;  // номинальная концентрация CaC₂, %

    // ── КПД: монотонно растёт от KPD_min до KPD_max за время TimeSliv ─────
    public double KPD_min { get; set; } = 0.76;   // начальный КПД
    public double KPD_max { get; set; } = 0.89;   // конечный КПД (достигается к моменту слива)

    // ── Электрические параметры ──────────────────────────────────────────
    public double I_el { get; set; } = 88000; // ток электрода, А (88 кА)
    public double Cos_phi { get; set; } = 0.68;  // коэффициент мощности
    public double R_nom { get; set; } = 0.045; // номинальное сопротивление, Ом
    public double E_nom { get; set; } = 246.0; // номинальная ЭДС, В

    // ── Производственные коэффициенты ────────────────────────────────────
    // K = 0.154 кг/(А·ч) — электрохимический эквивалент CaC₂
    // Ks = 25 — коэффициент расхода шихты для баланса уровня МПР
    // Ks_conc — коэффициент для формулы баланса концентрации (диплом, слайд 10)
    public double K { get; set; } = 0.154; // электрохимический эквивалент CaC₂, кг/(А·ч)
    public double Ks { get; set; } = 25;    // коэффициент расхода шихты (для баланса МПР)
    public double Ks_conc { get; set; } = 6.0;   // коэффициент для формулы концентрации

    // ── Коэффициенты чувствительности ────────────────────────────────────
    public double K_Rl { get; set; } = 0.000588;
    public double K_Rt { get; set; } = 0.000000000169;
    public double K_Rc { get; set; } = 0.0000656;
    public double K_Et { get; set; } = 0.00112;
    public double K_Ec { get; set; } = 0.0156;

    // ── Тепловые параметры ───────────────────────────────────────────────
    // P_melt подобран так, чтобы при L_mpr_nom=1.0 и T_nom=2000°C
    // джоулев нагрев уравновешивал теплоотдачу (отсутствие начального скачка T)
    public double P_melt { get; set; } = 0.099; // удельное сопротивление расплава, Ом·м
    public double A_raspl { get; set; } = 8.82;  // коэф. теплоотдачи расплав→зона плавления, Вт/(м²·°C)
    public double K_bottom { get; set; } = 60.0;  // коэф. теплоотдачи зона плавления→среда, Вт/(м²·°C)
    public double G_el { get; set; } = 12.0;  // скорость срабатывания электрода, м/ч

    // ── Параметры расчёта ────────────────────────────────────────────────
    public double DtStep { get; set; } = 0.01;   // шаг интегрирования, ч

    // ── Производные свойства ─────────────────────────────────────────────
    public double S_bath => FurnaceLength * FurnaceWidth;
    public double S_el => Math.PI * (D_el / 2) * (D_el / 2);
    public double S_bottom => S_bath + S_el;
    public double Mraspl_max => FurnaceLength * FurnaceWidth * H_melt_max * Rho_prod;
    public double Mraspl_residual => FurnaceLength * FurnaceWidth * H_melt_min * Rho_prod;
    // Время до слива при среднем КПД: в расплаве накапливается только CaC₂, CO улетает
    public double TimeSliv => (Mraspl_max - Mraspl_residual) / (((KPD_min + KPD_max) / 2.0) * I_el * K);

    // КПД линейно растёт от KPD_min (t=0) до KPD_max (t=TimeSliv)
    public double CalcKPD(double t)
    {
        double fraction = Math.Min(t / TimeSliv, 1.0);
        return KPD_min + (KPD_max - KPD_min) * fraction;
    }

    public double CalcGprod(double t) => CalcKPD(t) * I_el * K;

    //public MathModel(ModelParameters p)
    //{
    //    if (p == null) throw new ArgumentNullException(nameof(p));
    //    FurnaceLength = p.Length;
    //    FurnaceWidth = p.Width;
    //    H = p.H;
    //    D_el = p.Del;
    //    I_el = p.I * 1000.0;   // кА → А
    //    DtStep = p.dt;
    //}

    public double CalcR(double lmpr, double Traspl, double Cprod) =>
        R_nom + K_Rl * (lmpr - L_mpr_nom)
              - K_Rt * (Traspl - T_nom)
              + K_Rc * (Cprod - Cprod_nom);

    public double CalcE(double Traspl, double Cprod) =>
        E_nom - K_Et * (Traspl - T_nom)
              - K_Ec * Math.Log(Cprod / Cprod_nom);

    public double CalcU(double R, double E) => (I_el / 1000.0) * R + E;

    public double CalcQ(double U) => U * I_el * Cos_phi / 1000.0;

    // Формула баланса концентрации CaC₂ (диплом, слайд 10/17):
    // m_raspl · dC/dt = k · Ks_conc · I · η - Σ G_s
    // Используем Mraspl_max как постоянную базу (как в исходной статической модели),
    // чтобы события слива не вызывали скачков производной.
    private double Calc_dC(ControlInputs controls, double t)
    {
        double sumG = controls.G_lime + controls.G_coke;
        double gprod = CalcGprod(t);
        return (Ks_conc * gprod - sumG) / Mraspl_max;
    }

    private double Calc_dLmpr(ControlInputs controls)
    {
        double sumG = controls.G_lime + controls.G_coke;
        double v_el = G_el / (Rho_el * S_el);
        double v_prod = (sumG / Ks) / (Rho_prod * S_bath);
        return v_el - v_prod + controls.K_ctrl * controls.L_ctrl;
    }

    private double Calc_dTemp(double Traspl, double lmpr)
    {
        double R_melt = P_melt * lmpr / S_bath;
        return 3.6 * (I_el * I_el * R_melt
                     - A_raspl * S_bath * (Traspl - Tsmelt)
                     - K_bottom * S_bottom * (Traspl - T0))
                   / (Mraspl_max * Craspl);
    }

    public SimulationStep Advance(SimulationStep s, ControlInputs controls)
    {
        double t = s.Time;

        double R = CalcR(s.L_mpr, s.Temperature, s.C);
        double E = CalcE(s.Temperature, s.C);
        double U = CalcU(R, E);
        double Q = CalcQ(U);

        double newC = s.C + DtStep * Calc_dC(controls, t);
        double newLmpr = s.L_mpr + DtStep * Calc_dLmpr(controls);
        double newTemp = s.Temperature + DtStep * Calc_dTemp(s.Temperature, s.L_mpr);
        double newW = s.W + DtStep * Q;
        double newTime = s.Time + DtStep;
        double newMraspl = s.Mraspl + DtStep * CalcGprod(t); // только CaC₂ остаётся в расплаве, CO уходит

        double Rnew = CalcR(newLmpr, newTemp, newC);
        double Enew = CalcE(newTemp, newC);
        double Unew = CalcU(Rnew, Enew);
        double kpdNew = CalcKPD(newTime);

        return new SimulationStep
        {
            Time = newTime,
            C = newC,
            L_mpr = newLmpr,
            Temperature = newTemp,
            Mraspl = newMraspl,
            R = Rnew,
            E = Enew,
            U = Unew,
            Q = CalcQ(Unew),
            W = newW,
            Gprod = CalcGprod(newTime),
            KPD = kpdNew,
        };
    }

    public List<SimulationStep> RunSimulation(ControlInputs controls)
    {
        var current = InitialState();
        var results = new List<SimulationStep>();

        while (current.Mraspl < Mraspl_max)
        {
            results.Add(current);
            current = Advance(current, controls);
        }

        results.Add(current);
        return results;
    }

    public SimulationStep InitialState()
    {
        double R = CalcR(L_mpr_nom, T_nom, C_CaC2_0);
        double E = CalcE(T_nom, C_CaC2_0);
        double U = CalcU(R, E);
        double gprod = CalcGprod(0);
        return new SimulationStep
        {
            Time = 0,
            C = C_CaC2_0,
            L_mpr = L_mpr_nom,
            Temperature = T_nom,
            Mraspl = Mraspl_residual,
            R = R,
            E = E,
            U = U,
            Q = CalcQ(U),
            W = 0,
            Gprod = gprod,
            KPD = CalcKPD(0),
        };
    }
}
