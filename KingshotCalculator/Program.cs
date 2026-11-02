using KingshotCalculator;

CalculateAmadeusPetraRosa();
CalculateAlcarMargotSaul();
CalculateEricMargotSaul();

void CalculateAmadeusPetraRosa() => CalculateHeroes(Heroes.Amadeus, Heroes.Petra, Heroes.Rosa);
void CalculateAlcarMargotSaul() => CalculateHeroes(Heroes.Alcar, Heroes.Margot, Heroes.Saul);
void CalculateEricMargotSaul() => CalculateHeroes(Heroes.Eric, Heroes.Margot, Heroes.Saul);

void CalculateHeroes(Hero lead1, Hero lead2, Hero lead3)
{
    var topJoiners = SkillModCalculator.CalculateTopSkillMods(
        Heroes.HeroesList,
        lead1,
        lead2,
        lead3,
        10);

    Console.WriteLine($"Leads: {lead1.Name}, {lead2.Name}, {lead3.Name}");

    Console.WriteLine($"      Rally lead {lead1.Name}: {SkillModCalculator.FormatMultipliers(SkillModCalculator.GetLeadSkillMultipliers(lead1))}");
    Console.WriteLine($"      Rally lead {lead2.Name}: {SkillModCalculator.FormatMultipliers(SkillModCalculator.GetLeadSkillMultipliers(lead2))}");
    Console.WriteLine($"      Rally lead {lead3.Name}: {SkillModCalculator.FormatMultipliers(SkillModCalculator.GetLeadSkillMultipliers(lead3))}");

    var rank = 1;
    foreach (var (joiners, skillMod, multipliers) in topJoiners)
    {
        var joinerNames = string.Join(", ", joiners.Select(j => j.Name));
        Console.WriteLine($"  #{rank}: Joiners: {joinerNames} | Skill mod: {SkillModCalculator.FormatNumber(skillMod)}");
        //Console.WriteLine($"      Multipliers: {SkillModCalculator.FormatMultipliers(multipliers)}");
        rank++;
    }

    Console.WriteLine();
}