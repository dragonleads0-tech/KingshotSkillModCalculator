using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace KingshotCalculator
{
    public static class SkillModCalculator
    {
        public static (List<Hero> Joiners, double SkillMod, IReadOnlyDictionary<string, double> Multipliers) CalculateBestSkillMod(
            List<Hero> heroes,
            Hero lead1,
            Hero lead2,
            Hero lead3)
        {
            var top = CalculateTopSkillMods(heroes, lead1, lead2, lead3, 1);
            return top.Count == 0
                ? (new List<Hero>(), 0, new Dictionary<string, double>())
                : top[0];
        }

        public static List<(List<Hero> Joiners, double SkillMod, IReadOnlyDictionary<string, double> Multipliers)> CalculateTopSkillMods(
            List<Hero> heroes,
            Hero lead1,
            Hero lead2,
            Hero lead3,
            int count = 10)
        {
            if (heroes.Count == 0 || count <= 0)
                return new List<(List<Hero>, double, IReadOnlyDictionary<string, double>)>();

            var results = new List<(List<Hero> Joiners, double SkillMod, IReadOnlyDictionary<string, double> Multipliers)>();
            var heroCount = heroes.Count;

            for (var i = 0; i < heroCount; i++)
            for (var j = i; j < heroCount; j++)
            for (var k = j; k < heroCount; k++)
            for (var l = k; l < heroCount; l++)
            {
                var bestMod = double.MinValue;
                List<Hero>? bestJoiners = null;
                IReadOnlyDictionary<string, double>? bestMultipliers = null;

                foreach (var (joiner1, joiner2, joiner3, joiner4) in DistinctSlotOrders(
                             heroes[i], heroes[j], heroes[k], heroes[l]))
                {
                    var (mod, multipliers) = CalculateSkillMod(lead1, lead2, lead3, joiner1, joiner2, joiner3, joiner4);
                    if (mod > bestMod)
                    {
                        bestMod = mod;
                        bestJoiners = [joiner1, joiner2, joiner3, joiner4];
                        bestMultipliers = multipliers;
                    }
                }

                results.Add((bestJoiners!, bestMod, bestMultipliers!));
            }

            return results
                .OrderByDescending(r => r.SkillMod)
                .Take(count)
                .ToList();
        }

        private static List<(Hero Joiner1, Hero Joiner2, Hero Joiner3, Hero Joiner4)> DistinctSlotOrders(
            Hero joiner1,
            Hero joiner2,
            Hero joiner3,
            Hero joiner4)
        {
            var items = new[] { joiner1, joiner2, joiner3, joiner4 };
            var seenOrders = new HashSet<string>();
            var orders = new List<(Hero, Hero, Hero, Hero)>();
            CollectDistinctSlotOrders(items, 0, seenOrders, orders);
            return orders;
        }

        private static void CollectDistinctSlotOrders(
            Hero[] items,
            int start,
            HashSet<string> seenOrders,
            List<(Hero, Hero, Hero, Hero)> orders)
        {
            if (start == items.Length)
            {
                var orderKey = string.Join('|', items.Select(RuntimeHelpers.GetHashCode));
                if (seenOrders.Add(orderKey))
                    orders.Add((items[0], items[1], items[2], items[3]));
                return;
            }

            for (var i = start; i < items.Length; i++)
            {
                (items[start], items[i]) = (items[i], items[start]);
                CollectDistinctSlotOrders(items, start + 1, seenOrders, orders);
                (items[start], items[i]) = (items[i], items[start]);
            }
        }

        public static double CalculateSkillMod(Hero joiner1, Hero joiner2, Hero joiner3, Hero joiner4) 
        {
            var possibleSkills = new List<HeroSkill>
            {
                joiner1.Skill1,
                joiner2.Skill1,
                joiner3.Skill1,
                joiner4.Skill1,
            };

            return CalculateSkillMod(possibleSkills).SkillMod;
        }

        public static (double SkillMod, IReadOnlyDictionary<string, double> Multipliers) CalculateSkillMod(
            Hero lead1,
            Hero lead2,
            Hero lead3,
            Hero joiner1,
            Hero joiner2,
            Hero joiner3,
            Hero joiner4)
        {
            var possibleSkills = new List<HeroSkill>
            {
                lead1.Skill1,
                lead1.Skill2,
                lead1.Skill3,
                lead2.Skill1,
                lead2.Skill2,
                lead2.Skill3,
                lead3.Skill1,
                lead3.Skill2,
                lead3.Skill3,
                joiner1.Skill1,
                joiner2.Skill1,
                joiner3.Skill1,
                joiner4.Skill1
            };

            return CalculateSkillMod(possibleSkills);
        }

        public static string FormatNumber(double value) =>
            value.ToString("F2", CultureInfo.CurrentCulture);

        public static string FormatMultipliers(IReadOnlyDictionary<string, double> multipliers) =>
            multipliers.Count == 0
                ? "none"
                : string.Join(", ", multipliers.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}: {FormatNumber(entry.Value)}"));

        public static IReadOnlyDictionary<string, double> GetLeadSkillMultipliers(Hero lead) =>
            AggregateMultipliersFromSkills(lead.Skill1, lead.Skill2, lead.Skill3);

        private static (double SkillMod, IReadOnlyDictionary<string, double> Multipliers) CalculateSkillMod(List<HeroSkill> possibleSkills)
        {
            var skillsToImplement = new List<HeroSkill>();

            foreach (var possibleSkill in possibleSkills)
            {
                bool isStackable = true;

                foreach (var skill in skillsToImplement)
                {
                    if (!possibleSkill.IsStackableWithSkill(skill))
                    {
                        isStackable = false;
                        break;
                    }
                }

                if (isStackable)
                    skillsToImplement.Add(possibleSkill);
            }

            var multipliers = AggregateMultipliersFromSkills(skillsToImplement);
            var skillMod = 1.0;

            foreach (var multiplier in multipliers)
                skillMod *= 1 + multiplier.Value / 100;

            return (skillMod, multipliers);
        }

        private static Dictionary<string, double> AggregateMultipliersFromSkills(params HeroSkill[] skills) =>
            AggregateMultipliersFromSkills((IEnumerable<HeroSkill>)skills);

        private static Dictionary<string, double> AggregateMultipliersFromSkills(IEnumerable<HeroSkill> skills)
        {
            var multipliers = new Dictionary<string, double>();

            void UpdateMultiplier(string key, bool hasPercentChance, int percentChance, int value)
            {
                var result = hasPercentChance ? (double)percentChance / 100 * value : value;
                multipliers[key] = multipliers.GetValueOrDefault(key) + result;
            }

            foreach (var skill in skills)
            {
                if (skill.AttackUp != 0)
                    UpdateMultiplier(nameof(skill.AttackUp), skill.HasPercentChance, skill.PercentChance, skill.AttackUp);
                if (skill.DamageTakenDown != 0)
                    UpdateMultiplier(nameof(skill.DamageTakenDown), skill.HasPercentChance, skill.PercentChance, skill.DamageTakenDown);
                if (skill.DamageUp != 0)
                    UpdateMultiplier(nameof(skill.DamageUp), skill.HasPercentChance, skill.PercentChance, skill.DamageUp);
                if (skill.EnemyDamageTakenUp != 0)
                    UpdateMultiplier(nameof(skill.EnemyDamageTakenUp), skill.HasPercentChance, skill.PercentChance, skill.EnemyDamageTakenUp);
                if (skill.EnemyDamageDown != 0)
                    UpdateMultiplier(nameof(skill.EnemyDamageDown), skill.HasPercentChance, skill.PercentChance, skill.EnemyDamageDown);
                if (skill.LethalityUp != 0)
                    UpdateMultiplier(nameof(skill.LethalityUp), skill.HasPercentChance, skill.PercentChance, skill.LethalityUp);
                if (skill.DefenseUp != 0)
                    UpdateMultiplier(nameof(skill.DefenseUp), skill.HasPercentChance, skill.PercentChance, skill.DefenseUp);
                if (skill.HealthUp != 0)
                    UpdateMultiplier(nameof(skill.HealthUp), skill.HasPercentChance, skill.PercentChance, skill.HealthUp);
                if (skill.EnemyTroopsAttackDown != 0)
                    UpdateMultiplier(nameof(skill.EnemyTroopsAttackDown), skill.HasPercentChance, skill.PercentChance, skill.EnemyTroopsAttackDown);
                if (skill.DodgeAttacks != 0)
                    UpdateMultiplier(nameof(skill.DodgeAttacks), skill.HasPercentChance, skill.PercentChance, skill.DodgeAttacks);
                if(skill.EnemyLethalityDown != 0)
                    UpdateMultiplier(nameof(skill.EnemyLethalityDown), skill.HasPercentChance, skill.PercentChance, skill.EnemyLethalityDown);
            }

            return multipliers;
        }
    }

    public class Heroes
    {
        public static List<Hero> HeroesList => new List<Hero>
        {
            // remove amadeus since his joiner skill is the same as chenko
            // remove margot since his joiner skill is the same as amane
            Alcar, Rosa, Petra, /*Amadeus,*/ /*Margot,*/ Chenko, Amane, Saul, Hilde, Eric, Fahd
        };

        public static Hero Alcar { get; private set; } = new Hero
        {
            Name = "Alcar",
            Skill1 = new HeroSkill
            {
                DamageTakenDown = 20 * (60/100) // only for infantry and archers. Alcar is mostly used in garrison 60/40 formations, so weight it by troop ratio
            },
            Skill2 = new HeroSkill
            {
                DamageUp = 10 // 100 for infantry, 10 for archers/cavalry
            },
            Skill3 = new HeroSkill
            {
                EnemyDamageTakenUp = 25,
                DamageUp = 60 * (60/100) // 60 for infantry. Weight it by expected troop ratio(60/40)
            }
        };

        public static Hero Rosa { get; private set; } = new Hero
        {
            Name = "Rosa",
            Skill1 = new HeroSkill
            {
                HasPercentChance = true,
                PercentChance = 40,
                DamageUp = 50
            },
            Skill2 = new HeroSkill
            {
                EnemyDamageDown = 20
            },
            Skill3 = new HeroSkill
            {
                AttackUp = 30 // only archers
            }
        };

        public static Hero Petra { get; private set; } = new Hero
        {
            Name = "Petra",
            Skill1 = new HeroSkill
            {
                HasPercentChance = true,
                PercentChance = 50,
                EnemyDamageTakenUp = 50
            },
            Skill2 = new HeroSkill
            {
                HasPercentChance = true,
                PercentChance = 50,
                DamageUp = 50
            },
            Skill3 = new HeroSkill
            {
                HasPercentChance = true,
                PercentChance = 40,
                DamageTakenDown = 50
            }
        };

        public static Hero Amadeus { get; private set; } = new Hero
        {
            Name = "Amadeus",
            Skill1 = new HeroSkill
            {
                LethalityUp = 25
            },

            Skill2 = new HeroSkill
            {
                AttackUp = 25,
            },
            Skill3 = new HeroSkill
            {
                HasPercentChance = true,
                PercentChance = 40,
                DamageUp = 50
            }
        };

        public static Hero Margot { get; private set; } = new Hero
        {
            Name = "Margot",
            Skill1 = new HeroSkill
            {
                AttackUp = 25
            },
            Skill2 = new HeroSkill
            {
                HasPercentChance = true,
                PercentChance = 20,
                DodgeAttacks = 100
            },
            Skill3 = new HeroSkill
            {
                HasPercentChance = true,
                PercentChance = 25,
                DamageUp = 200 * (40/100) // only for cavalry, but margot will be used in 60/40. So weight it by this troop ratio
            }
        };

        public static Hero Jaeger { get; private set; } = new Hero
        {
            Name = "Jaeger",
            Skill1 = new HeroSkill
            {
                HasPercentChance = true,
                PercentChance = 20,
                DamageUp = 40
            },
            Skill2 = new HeroSkill
            {
                HasPercentChance = true,
                PercentChance = 20,
                EnemyLethalityDown = 50
            },
            Skill3 = new HeroSkill
            {
                HealthUp = 25
            }
        };

        public static Hero Chenko { get; private set; } = new Hero
        {
            Name = "Chenko",
            Skill1 = new HeroSkill
            {
                LethalityUp = 25
            }
        };

        public static Hero Amane { get; private set; } = new Hero
        {
            Name = "Amane",
            Skill1 = new HeroSkill
            {
                AttackUp = 25
            }
        };

        public static Hero Saul { get; private set; } = new Hero
        {
            Name = "Saul",
            Skill1 = new HeroSkill
            {
                DefenseUp = 10,
                HealthUp = 15
            },
            Skill2 = new HeroSkill
            {

            },
            Skill3 = new HeroSkill
            {
                LethalityUp = 25
            }
        };

        public static Hero Hilde { get; private set; } = new Hero
        {
            Name = "Hilde",
            Skill1 = new HeroSkill
            {
                AttackUp = 15,
                DefenseUp = 10
            }
        };


        public static Hero Fahd { get; private set; } = new Hero
        {
            Name = "Fahd",
            Skill1 = new HeroSkill
            {
                EnemyDamageDown = 20
            }
        };

        public static Hero Eric { get; private set; } = new Hero
        {
            Name = "Eric",
            Skill1 = new HeroSkill
            {
                EnemyTroopsAttackDown = 20
            },
            Skill2 = new HeroSkill
            {
                DamageTakenDown = 20
            },
            Skill3 = new HeroSkill
            {
                HealthUp = 25
            }
        };
    }

    public class Hero
    {
        public string Name { get; set; }

        public HeroSkill Skill1 { get; set; }
        public HeroSkill Skill2 { get; set; }
        public HeroSkill Skill3 { get; set; }
    }

    public class HeroSkill
    {
        public bool HasPercentChance { get; set; }
        public int PercentChance { get; set; }

        public int AttackUp { get; set; }
        public int DamageTakenDown { get; set; }
        public int DamageUp { get; set; }
        public int EnemyDamageTakenUp { get; set; }
        public int EnemyDamageDown { get; set; }
        public int LethalityUp { get; set; }
        public int DefenseUp { get; set; }
        public int HealthUp { get; set; }
        public int EnemyTroopsAttackDown { get; set; }
        public int DodgeAttacks { get; set; }
        public int EnemyLethalityDown { get; set; }

        public bool IsStackableWithSkill(HeroSkill skill)
        {
            return !HasPercentChance
                || AttackUp != 0 && skill.AttackUp == 0
                || DamageTakenDown != 0 && skill.DamageTakenDown == 0
                || DamageUp != 0 && skill.DamageUp == 0
                || EnemyDamageTakenUp != 0 && skill.EnemyDamageTakenUp == 0
                || EnemyDamageDown != 0 && skill.EnemyDamageDown == 0
                || LethalityUp != 0 && skill.LethalityUp == 0
                || DefenseUp != 0 && skill.DefenseUp == 0
                || HealthUp != 0 && skill.HealthUp == 0
                || EnemyTroopsAttackDown != 0 && skill.EnemyTroopsAttackDown == 0
                || DodgeAttacks != 0 && skill.DodgeAttacks == 0
                || EnemyLethalityDown != 0 && skill.EnemyLethalityDown == 0
                ;
        }
    }
}
