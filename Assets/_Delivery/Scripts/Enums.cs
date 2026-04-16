public enum Enums
{
    MainMenu,
    Game
}

public enum ResourceType
{
    Scraps,
    RawMaterials,
    HighEndComponents
}

public enum ClientFaction
{
    Corporation,
    Settler,
    BlackMarket,
    Government
}

public enum LocationType
{
    IndustrialRuin,     // Heavy Scraps              [fixed slot]
    MiningZone,         // Heavy RawMaterials        [fixed slot]
    FrontierColony,     // Mixed spread              [wildcard]
    CorporateOutpost,   // Heavy HighEndComponents   [wildcard]
    BlackSite,          // HighEnd + Scraps          [wildcard]
    MilitaryDepot,      // Raw + HighEnd             [wildcard]
    CrashSite           // Scraps + surprise HighEnd [wildcard]
}