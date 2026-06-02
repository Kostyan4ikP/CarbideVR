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

/// <summary>
/// Математическая модель карбидной печи (прямоугольная ванна).
/// Печь РПО-40 (40 МВА, Волгоград): длина 9.9 м, ширина 7.9 м.
/// Сырьё: известь + кокс. Целевой продукт: карбид кальция (CaC₂).
/// </summary>
public sealed class CalciumCarbideModel
{
    // ════════════════════════════════════════════════════════════════════
    //                         ПАРАМЕТРЫ МОДЕЛИ
    // ════════════════════════════════════════════════════════════════════

    // ── 1. Геометрия печи (прямоугольная) ──────────────────────────────
    // Карбидная печь 40 МВА (Волгоград): длина 9,9 × ширина 7,9 × высота 4,65 м.
    // Площадь ванны = L · W = 78,21 м².
    public double Length { get; set; } = 9.9;     // длина ванны, м
    public double Width { get; set; } = 7.9;     // ширина ванны, м
    public double H { get; set; } = 4.65;    // высота печи, м
    public double D_el { get; set; } = 1.5;     // диаметр электрода, м

    // ── 2. Физические свойства материалов ──────────────────────────────
    // Rho_prod — плотность жидкого карбидного расплава. По справочным
    // данным 2,22 т/м³ (= 2220 кг/м³) для CaC₂ при рабочей температуре.
    // M_max = S · H_max · ρ = 78,21 · 0,30 · 2220 ≈ 52 т — соответствует
    // оценке пользователя для печи 9,9 × 7,9 × 0,3 м.
    public double Rho_prod { get; set; } = 2220;

    public double Rho_el { get; set; } = 400;    // плотность электродной массы (Соберберг), кг/м³
                                                 // Теплоёмкость карбидного расплава: по справочнику ~0,27–0,31 кВт·ч/(т·°C)
                                                 // (см. таблицы 2.2, 2.4 в материалах). 0,27 хорошо согласуется с CaC₂.
    public double Craspl { get; set; } = 0.27;   // теплоёмкость расплава, кДж/(кг·°C)

    // ── 3. Начальное состояние и цикл накопления / слива ───────────────
    // H_max — рабочая высота расплава перед сливом; H_min — остаток.
    // Для карбидной печи 40 МВА (по материалам): h ванны 0,15–0,30 м,
    // M_max = π·(10/2)²·0,30·2220 ≈ 52 т ⇒ полностью соответствует
    // формуле m = S·h·ρ.
    public double H_max { get; set; } = 0.30;     // допустимая высота расплава, м (h_prod max)
    public double H_min { get; set; } = 0.2;     // остаточная высота после слива, м
    public double Mraspl_0 { get; set; } = 0;        // начальная масса (0 = взять из Mraspl_min)
    public double C_CaC2_0 { get; set; } = 65;     // начальная (= базовая) концентрация CaC₂, %

    // Слив (выпуск продукта). Высота расплава h_prod растёт со скоростью
    // v_prod = G_prod/(ρ·S) (см. формулы презентации, слайды 10, 17).
    // По достижении H_max печь выпускается до H_min. При сливе уносится
    // накопленный целевой продукт, и концентрация ключевого компонента
    // в оставшемся расплаве возвращается к базовой C_CaC2_0 (модель
    // выпуска товарного карбида — оставшийся «болото»-расплав имеет
    // исходный состав). Это согласовано с требованием: «при сливе C
    // возвращается примерно к исходной концентрации».

    // Mraspl_max / Mraspl_min — производные из геометрии: m = ρ·S·h
    public double Mraspl_max => Rho_prod * S_bath * H_max;
    public double Mraspl_min => Rho_prod * S_bath * H_min;
    public double Mraspl => Mraspl_max;

    // ── 4. Номинальный режим (точка линеаризации R и E) ────────────────
    // Карбидная реакция идёт при 1900–2000 °C, температура расплава
    // 1800–2300 °C (середина — 2050 °C).
    public double L_mpr_nom { get; set; } = 0.3;    // номинальное МПР, м 0.3
    public double T_nom { get; set; } = 2050;   // номинальная температура карбидного расплава, °C
    public double Tsmelt { get; set; } = 1800;   // температура зоны плавления / нижняя граница, °C
    public double T0 { get; set; } = 25;     // температура окружающей среды, °C
    public double Cprod_nom { get; set; } = 75.0;   // номинальная концентрация CaC₂ в продукте, %

    // ── 5. Производительность и шихтовые коэффициенты ──────────────────
    // Из материалов по получению CaC₂ (Ершова, диссертация, презентация):
    //   G_prod = K · I · η_ID                              (формула 2)
    //   m_raspl · dC_prod/dt = ΣG_s − Ks · K · I · η_ID    (формула 1)
    //
    // K — коэффициент выхода CaC₂ на ампер-час тока электрода.
    //   Целевая производительность 10–13 т/ч ≈ 12 т/ч при I = 88 кА, η = 0,88:
    //     K = G_prod/(I·η) = 12000/(88000·0,88) ≈ 0,155 кг/(А·ч).
    //   Это выше, чем у фосфора (0,0756), т.к. CaC₂ имеет другую электрохимию
    //   и большую долю полезного тока идёт на образование продукта.
    public double K { get; set; } = 0.155;   // коэффициент выхода CaC₂, кг/(А·ч)

    // Ks — стехиометрический коэффициент шихты: ΣG_сырья / G_prod.
    //   Из материального баланса: B (известь) = 0,912 т/т, A (кокс) = 0,557 т/т;
    //   ΣG = 0,912 + 0,557 ≈ 1,47 т/т → Ks ≈ 1,47.
    //   (для фосфора было 1,89 — сырья на 1 т продукта требуется меньше).
    public double Ks { get; set; } = 1.47;    // коэффициент шихты

    // ── 6. КПД как сглаженное случайное возмущение ────────────────────
    // Раз в KPD_changeInterval выбирается новая случайная «цель» в
    // [KPD_0, KPD_max], а текущий КПД ПЛАВНО к ней притягивается с
    // постоянной времени KPD_changeInterval.
    //
    // Для карбидной печи КПД ниже, чем у фосфорной:
    //   • Cos φ карб. печи ~0,7 (vs 0,92 у фосфорной);
    //   • активный КПД установки ~0,85–0,92 (по материалам — 0,88).
    public double KPD_0 { get; set; } = 0.85;
    public double KPD_max { get; set; } = 0.92;
    public double KPD_changeInterval { get; set; } = 0.5;   // 30 sim-минут

    // Сглаженный стохастический КПД: target меняется ступеньками,
    // _kpdCurrent релаксирует к нему.
    private double _kpdCurrent = 0.885;
    private double _kpdTarget = 0.885;
    private double _kpdNextChangeTime = -1.0;
    private double _kpdLastUpdateTime = double.NegativeInfinity;
    private readonly Random _kpdRng = new Random(42);

    // ── 7. Электрические параметры ─────────────────────────────────────
    // R в CalcU умножается на (I_el/1000) — эффективные единицы мОм (В/кА).
    //
    // Карбидная печь 40 МВА: линейный ток 88 кА (по материалам).
    // R_faz ≈ 1,4 мОм, U_рабочее = 130–280 В, U_полезное ≈ 102 В.
    // Cos φ карбидной печи ≈ 0,7 (низкий).
    public double I_el { get; set; } = 88000;  // ток электрода, А (88 кА)
    public double Cos_phi { get; set; } = 0.75;   // коэффициент мощности карб. печи
    public double R_nom { get; set; } = 1.4;    // мОм (R_фаз по материалам)
    public double E_nom { get; set; } = 100.0;  // В (E так, чтобы U = I·R + E попадал в 130–262 В)
    public double K_Rl { get; set; } = 1.5;    // мОм / м МПР
    public double K_Rt { get; set; } = 3e-4;   // мОм / °C (выше T → ниже R, эффект меньше из-за высоких T)
    public double K_Rc { get; set; } = 0.04;   // мОм / %
    public double K_Et { get; set; } = 0.04;   // В / °C
    public double K_Ec { get; set; } = 80.0;   // В, множитель ln(C/Cnom)

    // ── 8. Тепловые параметры ──────────────────────────────────────────
    // Тепловой баланс расплава (слайд 18 презентации):
    //   m·c·dT/dt = I²·R − α·S·(T−Tsmelt) − k_bottom·S_bottom·(T−T0)
    // Источник тепла — ОМИЧЕСКИЙ нагрев I²·R, где R — то же сопротивление
    // печи, что и в U = I·R + E (НЕ отдельное удельное сопротивление).
    // Поэтому поля P_melt (удельн. сопр. расплава) больше нет — нагрев
    // считается напрямую через R из CalcR. Тепло хим. реакций и потоков
    // принято пренебрежимо малым (допущение 3 презентации).
    public double A_raspl { get; set; } = 8.82;  // теплоотдача через стены, Вт/(м²·°C)
                                                 // k_bottom подобран так, чтобы при номинальном режиме (l=l_nom,
                                                 // T=2050, C=C_nom) тепловой баланс был равновесным: I²·R ≈ потери.
    public double K_bottom { get; set; } = 60;    // теплоотдача через подину, Вт/(м²·°C)

    // ── 9. Расход электродной массы ────────────────────────────────────
    // G_el подобран так, что скорость сгорания электрода v_el = G_el/(ρ_el·S_el)
    // примерно равна скорости подъёма расплава v_prod = G_prod/(ρ_prod·S).
    // Тогда МПР (dl/dt = v_el − v_prod) в номинале почти не дрейфует, и
    // им управляет оператор. Söderberg-расход ~4–6 кг на тонну продукта,
    // при 12 т/ч даёт ~49 кг/ч — физично.
    public double G_el { get; set; } = 216;        // кг/ч

    // ── 10. Физические границы МПР (страховочный клампинг) ─────────────
    public double L_mpr_min { get; set; } = 0; // 0.05
    public double L_mpr_max { get; set; } = 1.5; // 0.60

    // ── 11. Параметры расчёта ──────────────────────────────────────────
    public double DtStep { get; set; } = 0.01;    // шаг интегрирования, ч
                                                  // Число порций сырья за один цикл слива. Tau (интервал между
                                                  // загрузками) вычисляется как T_цикла / NPortionsPerCycle.
                                                  // 12 порций за цикл даёт 0.5–1 мин между загрузками при типичном
                                                  // цикле 6–12 мин, что хорошо смотрится в тренажёре.
    public double NPortionsPerCycle { get; set; } = 12;

    // Таймер импульсной загрузки сырья. Инициализируется в Advance при
    // первом вызове.
    private double _nextFeedTime = -1.0;

    // Tau (интервал между порциями) — используется И для UI-индикации,
    // И для шагов прироста C. Чем чаще подача — тем чаще C ступенчато
    // повышается. Жёстко ограничен диапазоном [TauMin, TauMax].
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
        // Жёсткие границы — иначе при netRate→0 Tau уходит в часы,
        // pulseMass = sumG·Tau становится огромной и UI-индикатор
        // мигает раз в полдня. Здесь Tau нужен только для UI.
        if (tau < TauMin) tau = TauMin;
        if (tau > TauMax) tau = TauMax;
        return tau;
    }

    // ── 12. Производные свойства ───────────────────────────────────────
    public double S_bath => Length * Width;                       // прямоугольная ванна
    public double S_el => Math.PI * (D_el / 2) * (D_el / 2);    // электрод круглый
    public double S_bottom => S_bath + S_el;
    // Номинальная производительность (для оценок типа TimeSliv) — берём KPD_0
    // как наименьший в диапазоне. Реальная Gprod каждого шага в Advance
    // считается через текущий случайный KPD.
    public double Gprod => KPD_0 * I_el * K;
    public double TimeSliv => Mraspl / Gprod;

    // ════════════════════════════════════════════════════════════════════
    //                         КОНСТРУКТОРЫ
    // ════════════════════════════════════════════════════════════════════

    public CalciumCarbideModel() { }
    // ════════════════════════════════════════════════════════════════════
    //                         БАЗОВЫЕ ФОРМУЛЫ ИЗ ИСТОЧНИКА
    //         (R, E, U, Q, КПД во времени — формулы 4–7 материалов)
    // ════════════════════════════════════════════════════════════════════

    // КПД ПЛАВНО блуждает в диапазоне [KPD_0, KPD_max]. Раз в
    // KPD_changeInterval выбирается новая случайная цель, а текущее
    // значение релаксирует к ней по закону первого порядка:
    //   dKPD/dt = (target − KPD) / τ,   τ ≈ KPD_changeInterval.
    // Идемпотентно по повторному вызову с тем же time.
    public double GetKPD(double time)
    {
        // Инициализация при первом вызове.
        if (_kpdNextChangeTime < 0)
        {
            _kpdNextChangeTime = time + KPD_changeInterval;
            _kpdLastUpdateTime = time;
            _kpdTarget = KPD_0 + _kpdRng.NextDouble() * (KPD_max - KPD_0);
        }

        // Новая цель — раз в KPD_changeInterval (while на случай большого dt).
        while (time >= _kpdNextChangeTime)
        {
            _kpdTarget = KPD_0 + _kpdRng.NextDouble() * (KPD_max - KPD_0);
            _kpdNextChangeTime += KPD_changeInterval;
        }

        // Плавная релаксация current → target с постоянной времени = interval.
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

    // ════════════════════════════════════════════════════════════════════
    //                  ДИФФЕРЕНЦИАЛЬНЫЕ УРАВНЕНИЯ СОСТОЯНИЯ
    // ════════════════════════════════════════════════════════════════════

    // Производительность ЭТУ (формула G_prod = k·I·η, слайды 10, 17).
    public double Gprod_of(double kpd) => K * I_el * kpd;   // кг/ч

    // Скорость сгорания электрода (слайд 17): v_el = G_el/(ρ_el·S_el).
    private double V_el() => G_el / (Rho_el * S_el);        // м/ч

    // Скорость подъёма уровня расплава (слайд 17): v_prod = G_prod/(ρ_prod·S).
    // ВНИМАНИЕ: используется НАСТОЯЩАЯ плотность продукта ρ_prod, никаких
    // «эффективных» плотностей. Уровень расплава растёт, т.к. продукт
    // непрерывно образуется со скоростью G_prod — независимо от моментов
    // подачи сырья.
    private double V_prod(double kpd) => Gprod_of(kpd) / (Rho_prod * S_bath);  // м/ч

    // Концентрация ключевого компонента (формула 1, слайды 10, 17):
    //   m_raspl · dC/dt = ΣG_s − k_s · G_prod
    //   ⇒ dC/dt = (ΣG_s − k_s · G_prod) / m_raspl
    // Решается методом Эйлера. Множитель 100 переводит долю → проценты
    // (C хранится и отображается в %). Расходы сырья ΣG_s входят НАПРЯМУЮ:
    // больше сырья → выше dC/dt. При балансе ΣG_s = k_s·G_prod концентрация
    // постоянна; при избытке — медленно растёт; при недокорме — падает.
    private double Calc_dC(double sumG, double meltMass, double kpd)
    {
        double mass = meltMass > 1.0 ? meltMass : 1.0;     // защита от /0
        return 100.0 * (sumG - Ks * Gprod_of(kpd)) / mass; // %/ч
    }

    // Высота слоя расплава (слайд 10/17): dh_prod/dt = v_prod.
    // Масса расплава потом считается как m = ρ_prod·S·h_prod (НЕ через ОДУ
    // массы). Это прямое следствие формул m_prod = ρ·S·h, h = v_prod·t.
    private double Calc_dH(double kpd) => V_prod(kpd);     // м/ч

    // МПР (слайд 10/17): dl_mpr/dt = v_el − v_prod + управление.
    private double Calc_dLmpr(ControlInputs controls, double kpd)
    {
        return V_el() - V_prod(kpd) + controls.K_ctrl * controls.L_ctrl;
    }

    // Температура расплава (слайд 18): тепловой баланс.
    //   m·c·dT/dt = I²·R − α·S·(T−Tsmelt) − k_bottom·S_bottom·(T−T0)
    // Источник тепла — омический нагрев I²·R, где R — сопротивление печи
    // (то же, что в U = I·R + E). R из CalcR в мОм → переводим в Ом (·1e-3).
    // Множитель 3.6 переводит Вт → кДж/ч (для согласования c [кДж/(кг·°C)]).
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

    // ════════════════════════════════════════════════════════════════════
    //                              ШАГ МОДЕЛИ
    // ════════════════════════════════════════════════════════════════════

    public SimulationStep Advance(SimulationStep s, ControlInputs controls)
    {
        double kpd = GetKPD(s.Time);
        // Шихта карбидной печи: ΣG_s = G_известь + G_кокс (расходы сырья, кг/ч)
        double sumG_rate = controls.G_izvest + controls.G_coks;
        if (sumG_rate < 0) sumG_rate = 0;

        // ── Событие FeedEvent: только для UI-подсветки порции ─────────
        // Физика подачи непрерывна (ΣG_s входит в dC/dt напрямую).
        double Tau = ComputeTau(sumG_rate, kpd);
        if (_nextFeedTime < 0.0) _nextFeedTime = Tau;
        bool feedEvent = false;
        while (s.Time >= _nextFeedTime)
        {
            feedEvent = true;
            _nextFeedTime += Tau;
        }

        // ── Высота слоя расплава h_prod (Эйлер): dh/dt = v_prod ───────
        // Уровень растёт непрерывно, т.к. продукт образуется со скоростью
        // G_prod (НЕ в момент подачи сырья). Масса = ρ·S·h_prod.
        double newH = s.H_prod + DtStep * Calc_dH(kpd);

        // ── Триггер слива: при h ≥ H_max выпуск до H_min ──────────────
        bool drainTriggered = false;
        if (newH >= H_max)
        {
            newH = H_min;
            drainTriggered = true;
        }
        if (newH < 0.0) newH = 0.0;

        // Масса расплава ВЫЧИСЛЯЕТСЯ из текущей высоты: m = ρ_prod·S·h.
        double meltMassPrev = Rho_prod * S_bath * s.H_prod;
        double newMeltMass = Rho_prod * S_bath * newH;

        // ── Концентрация (формула 1, метод Эйлера) ───────────────────
        //   dC/dt = (ΣG_s − k_s·G_prod) / m_raspl
        // При сливе — возврат к базовой концентрации C_CaC2_0 (выпуск
        // товарного продукта; в «болоте» остаётся исходный состав).
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

        // ── МПР (Эйлер): dl/dt = v_el − v_prod + управление ──────────
        double newLmpr = s.L_mpr + DtStep * Calc_dLmpr(controls, kpd);
        if (newLmpr < L_mpr_min) newLmpr = L_mpr_min;
        if (newLmpr > L_mpr_max) newLmpr = L_mpr_max;

        // ── Температура (Эйлер) ──────────────────────────────────────
        double newTemp = s.Temperature
            + DtStep * Calc_dTemp(s.Temperature, s.L_mpr, s.C, meltMassPrev);
        if (newTemp < T0) newTemp = T0;      // страховочные границы
        if (newTemp > 4000.0) newTemp = 4000.0;

        var gas = CarbideEmissions.CalculateMasses(Gprod_of(kpd));

        // ── Энергия за период ────────────────────────────────────────
        double newW = s.W + DtStep * CalcQ(CalcU(
            CalcR(s.L_mpr, s.Temperature, s.C),
            CalcE(s.Temperature, s.C)));

        // Производные на новом моменте
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


    // ════════════════════════════════════════════════════════════════════
    //                         ВХОДНЫЕ ТОЧКИ
    // ════════════════════════════════════════════════════════════════════

    public SimulationStep InitialState()
    {
        double kpd = GetKPD(0);
        double gprod = Gprod_of(kpd);
        double C0 = C_CaC2_0;
        // Начальная высота расплава: из Mraspl_0 (если задана) либо H_min.
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
