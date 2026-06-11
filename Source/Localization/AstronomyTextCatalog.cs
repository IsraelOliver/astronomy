namespace Astronomia;

public static class AstronomyTextCatalog
{
    public static string GetBodyName(Language language, string bodyName)
    {
        if (language != Language.English)
            return GetPortugueseBodyName(bodyName);

        return bodyName switch
        {
            "Sol" => "Sun",
            "Mercurio" => "Mercury",
            "Venus" => "Venus",
            "Terra" => "Earth",
            "Marte" => "Mars",
            "Jupiter" => "Jupiter",
            "Saturno" => "Saturn",
            "Urano" => "Uranus",
            "Netuno" => "Neptune",
            "Plutao" => "Pluto",
            "Lua" => "Moon",
            _ => bodyName,
        };
    }

    public static string GetPlanetSummary(Language language, CelestialBody planet)
    {
        if (language == Language.English)
            return GetEnglishPlanetSummary(planet.Name);

        return GetPortuguesePlanetSummary(planet.Name);
    }

    public static string GetSunType(Language language, SolarBody sun)
    {
        return language == Language.English
            ? "Yellow dwarf star"
            : "Estrela anã amarela";
    }

    public static string GetSunSummary(Language language, SolarBody sun)
    {
        return language == Language.English
            ? "Source of light and energy for the Solar System; it contains nearly all of the system's mass."
            : "Fonte de luz e energia do Sistema Solar; concentra quase toda a massa do sistema.";
    }

    private static string GetPortugueseBodyName(string bodyName)
    {
        return bodyName switch
        {
            "Mercurio" => "Mercúrio",
            "Venus" => "Vênus",
            "Jupiter" => "Júpiter",
            "Plutao" => "Plutão",
            _ => bodyName,
        };
    }

    private static string GetPortuguesePlanetSummary(string planetName)
    {
        return planetName switch
        {
            "Mercurio" => "Menor planeta e o mais próximo do Sol.",
            "Venus" => "Atmosfera muito densa e efeito estufa extremo.",
            "Terra" => "Nosso planeta, com água líquida estável na superfície.",
            "Marte" => "Planeta rochoso frio, com calotas polares e poeira rica em ferro.",
            "Jupiter" => "Maior planeta; sua gravidade influencia muitos corpos menores.",
            "Saturno" => "Gigante gasoso famoso por seus anéis extensos.",
            "Urano" => "Gigante gelado com eixo de rotação muito inclinado e anéis estreitos, escuros e difíceis de observar.",
            "Netuno" => "Gigante gelado distante, com ventos muito intensos.",
            "Plutao" => "Planeta anão distante, com órbita muito elíptica e inclinada em relação aos planetas principais.",
            _ => string.Empty,
        };
    }

    private static string GetEnglishPlanetSummary(string planetName)
    {
        return planetName switch
        {
            "Mercurio" => "The smallest planet and the closest one to the Sun.",
            "Venus" => "A world with an extremely dense atmosphere and a runaway greenhouse effect.",
            "Terra" => "Our planet, with stable liquid water on its surface.",
            "Marte" => "A cold rocky planet with polar caps and iron-rich dust.",
            "Jupiter" => "The largest planet; its gravity influences many smaller bodies.",
            "Saturno" => "A gas giant famous for its broad ring system.",
            "Urano" => "An ice giant with a highly tilted rotation axis and narrow, dark rings that are difficult to observe.",
            "Netuno" => "A distant ice giant with extremely intense winds.",
            "Plutao" => "A distant dwarf planet with a highly elliptical orbit inclined relative to the main planets.",
            _ => string.Empty,
        };
    }
}
