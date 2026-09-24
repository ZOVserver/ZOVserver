using System.Collections.Frozen;
using Humanizer;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Tables;

public static class DataTablesInfo
{
    static DataTablesInfo()
    {
        var classIds = new Dictionary<string, int>
        {
            { "texts_patch.csv", 0 },
            { "locales.csv", 1 },
            { "billing_packages.csv", 2 },
            { "globals.csv", 3 },
            { "sounds.csv", 4 },
            { "resources.csv", 5 },
            { "projectiles.csv", 6 },
            { "effects.csv", 7 },
            { "alliance_badges.csv", 8 },
            { "client_globals.csv", 9 },
            { "particle_emitters.csv", 10 },
            { "health_bars.csv", 11 },
            { "music.csv", 12 },
            { "credits.csv", 13 },
            { "regions.csv", 14 },
            { "locations.csv", 15 },
            { "characters.csv", 16 },
            { "area_effects.csv", 17 },
            { "items.csv", 18 },
            { "maps.csv", 19 },
            { "skills.csv", 20 },
            { "campaign.csv", 21 },
            { "bosses.csv", 22 },
            { "cards.csv", 23 },
            { "animations.csv", 24 },
            { "alliance_roles.csv", 25 },
            { "tutorial.csv", 26 },
            { "tiles.csv", 27 },
            { "player_thumbnails.csv", 28 },
            { "skins.csv", 29 },
            { "faces.csv", 30 },
            { "pins.csv", 35 },
            { "hints.csv", 36 },
            { "map_blocks.csv", 37 },
            { "skinsrarity.csv", 38 },
            { "milestones.csv", 39 },
            { "messages.csv", 40 },
            { "themes.csv", 41 },
            { "links.csv", 42 },
            { "name_colors.csv", 43 },
            { "skin_confs.csv", 44 },
            { "shop_items.csv", 45 },
            { "color_gradients.csv", 46 },
            { "location_themes.csv", 47 },
            { "game_mode_variations.csv", 48 },
            { "challenges.csv", 49 },
            { "accessories.csv", 50 },
            { "local_notifications.csv", 51 },
            { "emotes.csv", 52 },
            { "emote_bundles.csv", 53 },
            { "player_map_environments.csv", 54 },
            { "map_templates.csv", 55 },
            { "seasonal_skin_sections.csv", 56 },
            { "skin_campaigns.csv", 57 },
            { "ranked_ranks.csv", 58 },
            { "ranked_locations.csv", 59 },
            { "carryables.csv", 60 },
            { "gear_boosts.csv", 61 },
            { "gear_levels.csv", 62 },
            { "alliance_league_modes.csv", 63 },
            { "alliance_league_ranks.csv", 64 },
            { "bp_purchase_popup.csv", 65 },
            { "location_features.csv", 66 },
            { "login_calendar_items.csv", 67 },
            { "sprays.csv", 68 },
            { "shop_panel_layouts.csv", 69 },
            { "shop_style_sets.csv", 70 },
            { "gear_rarities.csv", 71 },
            { "fame_tiers.csv", 72 },
            { "mastery_levels.csv", 73 },
            { "mastery_hero_confs.csv", 74 },
            { "mastery_points.csv", 75 },
            { "player_titles.csv", 76 },
            { "catalog_collections.csv", 77 },
            { "battle_feats.csv", 78 },
            { "random_rewards.csv", 79 },
            { "random_reward_containers.csv", 80 },
            { "club_piggy_wins.csv", 81 },
            { "club_piggy_levels.csv", 82 },
            { "enumerated_id_lists.csv", 83 },
            { "ad_placements.csv", 84 },
            { "player_frames.csv", 85 },
            { "skin_rarities.csv", 86 },
            { "status_effects.csv", 87 },
            { "ranked_star_rewards.csv", 88 },
            { "collabs.csv", 89 },
            { "class_archetypes.csv", 90 },
            { "night_market_bundles.csv", 91 },
            { "night_market_items.csv", 92 },
            { "event_slots.csv", 93 },
            { "skin_anim_sequences.csv", 94 },
            { "intro_flows.csv", 95 },
            { "trophy_season_reward_levels.csv", 96 }
        };

        ClassIds = classIds.ToFrozenDictionary();
        ClassIds2 = ClassIds.ToDictionary(c => c.Value, c => c.Key).ToFrozenDictionary();
    }

    private static FrozenDictionary<string, int> ClassIds { get; }
    private static FrozenDictionary<int, string> ClassIds2 { get; }

    public static int GetTableIdByCsvName(string csvName)
    {
        if (!csvName.EndsWith(".csv"))
            csvName += ".csv";

        return ClassIds.GetValueOrDefault(csvName, -1);
    }

    public static string GetCsvNameByCsvId(int csvId)
    {
        return ClassIds2.GetValueOrDefault(csvId, string.Empty);
    }

#pragma warning disable S6640
    public static unsafe string GetTableNameByCsvName(string csvName)
    {
        var fileName = csvName;

        fileName = fileName.Singularize();

        fixed (char* d = fileName)
        {
            *d = (*d).ToString().ToUpper()[0];

            for (var j = 0; j < fileName.Length; j++)
                if (fileName[j] == '_')
                    *(d + j + 1) = (*(d + j + 1)).ToString().ToUpper()[0];

            fileName = fileName.Replace("_", "");
        }

        return $"Logic{fileName}Data";
    }
#pragma warning restore S6640

    public static string GetTableNameByCsvId(int csvId)
    {
        return GetTableNameByCsvName(GetCsvNameByCsvId(csvId).Replace(".csv", ""));
    }

    public static string GetTableFilenameByPath(string path)
    {
        var lastSeparatorIndex = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
        var dotIndex = path.IndexOf('.', lastSeparatorIndex);

        return path[(lastSeparatorIndex + 1)..dotIndex];
    }
}