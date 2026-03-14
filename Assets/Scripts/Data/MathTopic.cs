namespace VacuumVille.Data
{
    public enum MathTopic
    {
        Counting1To10,
        Counting1To20,
        AdditionTo10,
        SubtractionWithin10,
        AdditionTo20,
        SubtractionWithin20,
        Multiplication2x5x,
        DivisionBy2_3_5,
        NumberOrdering,
        ShapeCounting,
        MixedReview
    }

    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    public enum GameState
    {
        Boot,
        LanguageSelect,
        Home,
        LevelSelect,
        LevelIntro,
        CharacterSelect,
        MathTask,
        MinigameUnlock,
        Minigame,
        LevelComplete,
        ParentDashboard,
        Settings,
        Achievements
    }

    public enum MinigameType
    {
        SockSortSweep,
        CrumbCollectCountdown,
        CushionCannonCatch,
        DrainDefense,
        BoxTowerBuilder,
        StreamerUntangleSprint,
        FlowerBedFrenzy,
        AtticBinBlitz,
        SequenceSprinkler,
        GrandHallRestoration,
        ScramblerShutdown
    }

    public enum CharacterType
    {
        Rumble,
        Xixi,
        Rocky,
        Bubbles,
        Max,
        Zara,
        Turbo,
        ProfessorPebble,
        Luna
    }

    public enum Language
    {
        Czech,
        English
    }
}
