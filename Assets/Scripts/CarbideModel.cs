using System;
using System.Collections.Generic;

public sealed class SimulationStep
{
    public double Time { get; set; }
    public double C { get; set; }   // концентрация продукта CaC₂, %
    public double H_prod { get; set; }   // высота слоя расплава, м (h_prod)
    public double L_mpr { get; set; }   // МПР, м
    public double Temperature { get; set; }   // °C
    public double R { get; set; }   // мОм
    public double E { get; set; }   // ЭДС, В
    public double U { get; set; }   // напряжение, В
    public double Q { get; set; }   // мощность, кВт
    public double W { get; set; }   // накопленная энергия, кВт·ч
    public double MeltMass { get; set; }   // масса расплава, кг (= ρ·S·h_prod)
    public double Gprod { get; set; }   // производительность по CaC₂, кг/ч
    public double KPD_eff { get; set; }   // текущий КПД
    public bool DrainEvent { get; set; }   // на этом шаге сработал слив
    public bool FeedEvent { get; set; }   // на этом шаге сработала загрузка порции сырья
    public double CO { get; set; }
    public double CO2 { get; set; }
    public double H2 { get; set; }
}

public sealed class CalciumCarbideModel
{
    public double Length { get; set; } = 9.9;     // длина ванны, м
    public double Width { get; set; } = 7.9;     // ширина ванны, м
    public double H { get; set; } = 4.65;    // высота печи, м
    public double D_el { get; set; } = 1.5;     // диаметр электрода, м

    public double Rho_prod { get; set; } = 2220;

    public double Rho_el { get; set; } = 400;
    public double Craspl { get; set; } = 0.27;

    public double H_max { get; set; } = 0.30;
    public double H_min { get; set; } = 0.2;
    public double Mraspl_0 { get; set; } = 0;
    public double C_CaC2_0 { get; set; } = 65;

    public double Mraspl_max => Rho_prod * S_bath * H_max;
    public double Mraspl_min => Rho_prod * S_bath * H_min;
    public double Mraspl => Mraspl_max;

    public double L_mpr_nom { get; set; } = 0.3;
    public double T_nom { get; set; } = 2050;
    public double Tsmelt { get; set; } = 1800;
    public double T0 { get; set; } = 25;
    public double Cprod_nom { get; set; } = 75.0;
    
    public double K { get; set; } = 0.155;   // Электрохим эквивалент, кг/(А·ч)
    public double Ks { get; set; } = 1.47;    // коэффициент шихты

    public double KPD_0 { get; set; } = 0.85;
    public double KPD_max { get; set; } = 0.92;
    public double KPD_changeInterval { get; set; } = 0.5;


    private double _kpdCurrent = 0.885;
    private double _kpdTarget = 0.885;
    private double _kpdNextChangeTime = -1.0;
    private double _kpdLastUpdateTime = double.NegativeInfinity;
    private readonly Random _kpdRng = new Random(42);

    public double I_el { get; set; } = 88000;  // ток электрода, А (88 кА)
    public double Cos_phi { get; set; } = 0.75;   // коэффициент мощности карб. печи
    public double R_nom { get; set; } = 1.4;    // мОм (R_фаз по материалам)
    public double E_nom { get; set; } = 100.0;  // В 
    public double K_Rl { get; set; } = 1.5;    // мОм/м
    public double K_Rt { get; set; } = 3e-4;   // мОм/°C 
    public double K_Rc { get; set; } = 0.04;   // мОм/%
    public double K_Et { get; set; } = 0.04;   // В/°C
    public double K_Ec { get; set; } = 80.0;   // В, 

    public double A_raspl { get; set; } = 8.82;  // теплоотдача через стены, Вт/(м²·°C)
    public double K_bottom { get; set; } = 60;    // теплоотдача через подину, Вт/(м²·°C)

    public double G_el { get; set; } = 216; // кг/ч

    public double L_mpr_min { get; set; } = 0;
    public double L_mpr_max { get; set; } = 1.5;

    // ── 11. Параметры расчёта ──────────────────────────────────────────
    public double DtStep { get; set; } = 0.01;  // шаг интегрирования

    public double NPortionsPerCycle { get; set; } = 12;

    private double _nextFeedTime = -1.0;
    public double TauMin { get; set; } = 0.05;   // ≥ 3 мин между порциями
    public double TauMax { get; set; } = 0.15;   // ≤ 9 мин между порциями (для видимой динамики)
    private double ComputeTau(double sumG, double kpd)
    {
        double netRate = sumG - Ks * K * I_el * kpd;
        double cycleRange = Mraspl_max - Mraspl_min;
        double tau;
        if (netRate > 100.0)
            tau = cycleRange / (NPortionsPerCycle * netRate);
        else if (sumG > 1.0)
            tau = cycleRange / (NPortionsPerCycle * sumG);
        else
            tau = TauMax;
        if (tau < TauMin) tau = TauMin;
        if (tau > TauMax) tau = TauMax;
        return tau;
    }
    public double S_bath => Length * Width;                     // прямоугольная ванна
    public double S_el => Math.PI * (D_el / 2) * (D_el / 2);    // электрод круглый
    public double S_bottom => S_bath + S_el;

    public double Gprod => KPD_0 * I_el * K;
    public double TimeSliv => Mraspl / Gprod;


    public CalciumCarbideModel() { }

    public double GetKPD(double time)
    {
        if (_kpdNextChangeTime < 0)
        {
            _kpdNextChangeTime = time + KPD_changeInterval;
            _kpdLastUpdateTime = time;
            _kpdTarget = KPD_0 + _kpdRng.NextDouble() * (KPD_max - KPD_0);
        }

        while (time >= _kpdNextChangeTime)
        {
            _kpdTarget = KPD_0 + _kpdRng.NextDouble() * (KPD_max - KPD_0);
            _kpdNextChangeTime += KPD_changeInterval;
        }

        double dt = time - _kpdLastUpdateTime;
        if (dt > 0)
        {
            double tau = KPD_changeInterval;
            double alpha = 1.0 - Math.Exp(-dt / tau);
            _kpdCurrent += alpha * (_kpdTarget - _kpdCurrent);
            _kpdLastUpdateTime = time;
        }
        return _kpdCurrent;
    }

    public double CalcR(double lmpr, double Traspl, double Cprod) =>
        R_nom + K_Rl * (lmpr - L_mpr_nom)
              - K_Rt * (Traspl - T_nom)
              + K_Rc * (Cprod - Cprod_nom);

    public double CalcE(double Traspl, double Cprod) =>
        E_nom - K_Et * (Traspl - T_nom)
              - K_Ec * Math.Log(Cprod / Cprod_nom);

    public double CalcU(double R, double E) => (I_el / 1000.0) * R + E;

    public double CalcQ(double U) => U * I_el * Cos_phi / 1000.0;

    public double Gprod_of(double kpd) => K * I_el * kpd;   // кг/ч
    private double V_el() => G_el / (Rho_el * S_el);        // м/ч
    private double V_prod(double kpd) => Gprod_of(kpd) / (Rho_prod * S_bath);  // м/ч
    private double Calc_dC(double sumG, double meltMass, double kpd)
    {
        double mass = meltMass > 1.0 ? meltMass : 1.0;     // защита от /0
        return 100.0 * (sumG - Ks * Gprod_of(kpd)) / mass; // %/ч
    }

    private double Calc_dH(double kpd) => V_prod(kpd);     // м/ч

    private double Calc_dLmpr(ControlInputs controls, double kpd)
    {
        return V_el() - V_prod(kpd) + controls.K_ctrl * controls.L_ctrl;
    }

    private double Calc_dTemp(double Traspl, double lmpr, double Cprod, double meltMass)
    {
        double R_ohm = CalcR(lmpr, Traspl, Cprod) * 1e-3;     // мОм → Ом
        double heatW = I_el * I_el * R_ohm;                   // Вт (омический нагрев)
        double mass = meltMass > 1.0 ? meltMass : 1.0;
        return 3.6 * (heatW
                     - A_raspl * S_bath * (Traspl - Tsmelt)
                     - K_bottom * S_bottom * (Traspl - T0))
                   / (mass * Craspl);
    }

    public SimulationStep Advance(SimulationStep s, ControlInputs controls)
    {
        double kpd = GetKPD(s.Time);

        double sumG_rate = controls.G_izvest + controls.G_coks;
        if (sumG_rate < 0) sumG_rate = 0;

        double Tau = ComputeTau(sumG_rate, kpd);
        if (_nextFeedTime < 0.0) _nextFeedTime = Tau;
        bool feedEvent = false;
        while (s.Time >= _nextFeedTime)
        {
            feedEvent = true;
            _nextFeedTime += Tau;
        }

        double newH = s.H_prod + DtStep * Calc_dH(kpd);

        bool drainTriggered = false;
        if (newH >= H_max)
        {
            newH = H_min;
            drainTriggered = true;
        }
        if (newH < 0.0) newH = 0.0;

        double meltMassPrev = Rho_prod * S_bath * s.H_prod;
        double newMeltMass = Rho_prod * S_bath * newH;

        double newC;
        if (drainTriggered)
        {
            newC = C_CaC2_0;
        }
        else
        {
            newC = s.C + DtStep * Calc_dC(sumG_rate, meltMassPrev, kpd);
        }
        if (newC < 0.0) newC = 0.0;
        if (newC > 100.0) newC = 100.0;

        double newLmpr = s.L_mpr + DtStep * Calc_dLmpr(controls, kpd);
        if (newLmpr < L_mpr_min) newLmpr = L_mpr_min;
        if (newLmpr > L_mpr_max) newLmpr = L_mpr_max;

        double newTemp = s.Temperature
            + DtStep * Calc_dTemp(s.Temperature, s.L_mpr, s.C, meltMassPrev);
        if (newTemp < T0) newTemp = T0;      // страховочные границы
        if (newTemp > 4000.0) newTemp = 4000.0;

        var gas = CarbideEmissions.CalculateMasses(Gprod_of(kpd));

        double newW = s.W + DtStep * CalcQ(CalcU(
            CalcR(s.L_mpr, s.Temperature, s.C),
            CalcE(s.Temperature, s.C)));

        double newTime = s.Time + DtStep;
        double kpdNew = GetKPD(newTime);
        double gprodNew = Gprod_of(kpdNew);
        double Rnew = CalcR(newLmpr, newTemp, newC);
        double Enew = CalcE(newTemp, newC);
        double Unew = CalcU(Rnew, Enew);

        return new SimulationStep
        {
            Time = newTime,
            C = newC,
            H_prod = newH,
            L_mpr = newLmpr,
            Temperature = newTemp,
            R = Rnew,
            E = Enew,
            U = Unew,
            Q = CalcQ(Unew),
            W = newW,
            MeltMass = newMeltMass,
            Gprod = gprodNew,
            KPD_eff = kpdNew,
            DrainEvent = drainTriggered,
            FeedEvent = feedEvent,
            CO = gas.MassCO,
            CO2 = gas.MassCO2,
            H2 = gas.MassH2,
        };
    }

    public SimulationStep InitialState()
    {
        double kpd = GetKPD(0);
        double gprod = Gprod_of(kpd);
        double C0 = C_CaC2_0;
        double H0 = Mraspl_0 > 0 ? Mraspl_0 / (Rho_prod * S_bath) : H_min;
        double M0 = Rho_prod * S_bath * H0;
        double R = CalcR(L_mpr_nom, T_nom, C0);
        double E = CalcE(T_nom, C0);
        double U = CalcU(R, E);
        double CO = 0;
        double CO2 = 0;
        double H2 = 0;
        return new SimulationStep
        {
            Time = 0,
            C = C0,
            H_prod = H0,
            L_mpr = L_mpr_nom,
            Temperature = T_nom,
            R = R,
            E = E,
            U = U,
            Q = CalcQ(U),
            W = 0,
            MeltMass = M0,
            Gprod = gprod,
            KPD_eff = kpd,
            DrainEvent = false,
            CO = CO,
            CO2 = CO2,
            H2 = H2,
        };
    }

    public List<SimulationStep> RunSimulation(ControlInputs controls)
    {
        var current = InitialState();
        var results = new List<SimulationStep>(capacity: (int)(TimeSliv / DtStep) + 1);

        while (current.Time < TimeSliv)
        {
            results.Add(current);
            current = Advance(current, controls);
        }

        results.Add(current);
        return results;
    }
}

public struct EmissionMasses
{
    public double MassCO;
    public double MassCO2;
    public double MassH2;
}

public static class CarbideEmissions
{
    private const double M_CaC2 = 64;
    private const double M_CO = 28;
    private const double M_CO2 = 44;
    private const double M_H2 = 2;

    // --- 2. Коэффициенты выхода по реакциям (доли) ---
    private const double Yield_j1 = 0.80; // Основная реакция (CaO + 3C → CaC₂ + CO)
    private const double Yield_j2 = 0.10; // Побочная реакция (2CaO + CaC₂ → 3Ca + 2CO)
    private const double Yield_j3 = 0.05; // Реакция с влагой (C + H₂O → H₂ + CO)
    private const double Yield_j4 = 0.05; // Окисление (2CaC₂ + 5O₂ → 2CaO + 4CO₂)

    private const double MOISTURE_RATIO = 0.15; // Моли влаги, вступающей в реакцию, на 1 моль CaC₂

    public static double GetMassCO(double G_CaC2)
    {
        double n_CaC2 = (G_CaC2 * 1e6) / M_CaC2;

        double n_CO = n_CaC2 * (
            (1.0 * Yield_j1) +                                           // Реакция 1: 1 моль CO
            (2.0 * Yield_j2) +                                           // Реакция 2: 2 моля CO
            (1.0 * MOISTURE_RATIO * Yield_j3)                            // Реакция 3: 1 моль CO на моль влаги
        );

        return (n_CO * M_CO) / 1e6;
    }

    public static double GetMassCO2(double G_CaC2)
    {
        double n_CaC2 = (G_CaC2 * 1e6) / M_CaC2;

        // Реакция 4: 4 моля CO₂ на 2 моля CaC₂ = коэффициент 2.0
        double n_CO2 = n_CaC2 * (2.0 * Yield_j4);

        return (n_CO2 * M_CO2) / 1e6;
    }

    public static double GetMassH2(double G_CaC2)
    {
        double n_CaC2 = (G_CaC2 * 1e6) / M_CaC2;

        // Реакция 3: 1 моль H₂ на 1 моль прореагировавшей влаги
        double n_H2 = n_CaC2 * (1.0 * MOISTURE_RATIO * Yield_j3);

        return (n_H2 * M_H2) / 1e6;
    }

    public static EmissionMasses CalculateMasses(double G_CaC2)
    {
        return new EmissionMasses
        {
            MassCO = GetMassCO(G_CaC2),
            MassCO2 = GetMassCO2(G_CaC2),
            MassH2 = GetMassH2(G_CaC2)
        };
    }
}
