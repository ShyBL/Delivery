// ============================================================
//  Enums.cs
//  Place in: Assets/_Delivery/Scripts/
// ============================================================

public enum GameState
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
    IndustrialRuin,
    MiningZone,
    FrontierColony,
    CorporateOutpost,
    BlackSite,
    MilitaryDepot,
    CrashSite
}

public enum GamePhase
{
    Board,
    Scavenging
}
