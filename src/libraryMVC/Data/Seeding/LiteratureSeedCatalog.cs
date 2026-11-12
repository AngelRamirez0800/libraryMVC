namespace libraryMVC.Data.Seeding;

public sealed record LiteratureCategorySeed(string Name, string Description);

public sealed record LiteratureBookSeed(
    string Isbn,
    string Title,
    string Summary,
    IReadOnlyList<string> Categories,
    string? ImageFileName);

public sealed record LiteratureAuthorSeed(
    string FirstName,
    string LastName,
    string Nationality,
    DateTime BirthDate,
    string PortraitFileName,
    IReadOnlyList<LiteratureBookSeed> Books);

public static class LiteratureSeedCatalog
{
    public static IReadOnlyList<LiteratureCategorySeed> Categories
    {
        get;
    } =
    [
        new("Classic Literature", "Enduring works recognized as literary classics."),
        new("Novel", "Long-form works of prose fiction."),
        new("Short Story Collection", "Collections of short literary fiction."),
        new("Psychological Fiction", "Fiction focused on inner life and motivation."),
        new("Surrealist Fiction", "Fiction using dreamlike or disorienting situations."),
        new("Absurdist Fiction", "Fiction exploring absurdity and uncertain meaning."),
        new("Bureaucratic Fiction", "Fiction about opaque institutions and bureaucracy."),
        new("Mystery", "Stories structured around an unexplained event or puzzle."),
        new("Detective Fiction", "Fiction centered on the investigation of a crime."),
        new("Crime Fiction", "Fiction involving crimes and their consequences."),
        new("Gothic Fiction", "Fiction using dark, suspenseful, or uncanny elements."),
        new("Adventure", "Fiction centered on exploration and dangerous journeys."),
        new("Science Fiction", "Fiction using speculative scientific or prehistoric settings."),
        new("Coming-of-Age", "Fiction about a young person's development."),
        new("Migration Fiction", "Fiction about travel, displacement, and belonging."),
        new("Letters and Correspondence", "Published personal letters and correspondence."),
        new("Nonfiction", "Works based on factual material rather than invented narrative."),
        new("Philosophical Fiction", "Fiction exploring philosophical questions."),
        new("Political Fiction", "Fiction focused on political movements or conflict."),
        new("Political Satire", "Fiction using satire to examine political ideas."),
        new("Family Saga", "Fiction tracing a family and its conflicts."),
        new("Religious Fiction", "Fiction exploring religious belief and doubt."),
        new("Social Satire", "Fiction satirizing social conventions."),
        new("Prison Literature", "Literature shaped by confinement and penal life."),
        new("Memoir", "First-person recollection of lived experience."),
        new("Historical Fiction", "Fiction set against a historical period."),
        new("Gambling Fiction", "Fiction involving gambling and its effects.")
    ];

    public static IReadOnlyList<LiteratureAuthorSeed> Authors
    {
        get;
    } =
    [
        new(
            "Franz",
            "Kafka",
            "Austro-Hungarian (Bohemian)",
            new DateTime(1883, 7, 3),
            "franz-kafka.jpg",
            [
                new(
                    "9780805208498",
                    "The Metamorphosis; The Penal Colony, and Other Stories",
                    "A collection pairing Kafka's transformation tale with a story about a remote penal colony and other short prose.",
                    ["Short Story Collection", "Classic Literature", "Psychological Fiction"],
                    "kafka-metamorphosis.jpg"),
                new(
                    "9780805209990",
                    "The Trial",
                    "Josef K. is arrested without explanation and struggles to understand an opaque judicial process.",
                    ["Novel", "Classic Literature", "Psychological Fiction", "Absurdist Fiction", "Bureaucratic Fiction"],
                    "kafka-trial.jpg"),
                new(
                    "9780805211061",
                    "The Castle",
                    "A land surveyor known as K. tries to gain access to a castle and its elusive authorities.",
                    ["Novel", "Classic Literature", "Absurdist Fiction", "Bureaucratic Fiction"],
                    "kafka-castle.jpg"),
                new(
                    "9780805210644",
                    "Amerika",
                    "The unfinished novel follows young Karl Rossmann through a series of encounters in America.",
                    ["Novel", "Classic Literature", "Migration Fiction", "Coming-of-Age"],
                    "kafka-amerika.jpg"),
                new(
                    "9788090217140",
                    "A Country Doctor",
                    "A doctor's urgent winter call becomes a sequence of surreal and unsettling predicaments.",
                    ["Short Story Collection", "Surrealist Fiction", "Classic Literature"],
                    null),
                new(
                    "9781473555921",
                    "Letters to Milena",
                    "A selection of Kafka's correspondence with Milena Jesenská, written during the early 1920s.",
                    ["Letters and Correspondence", "Nonfiction", "Classic Literature"],
                    null),
                new(
                    "9780811226899",
                    "Investigations of a dog, and other creatures",
                    "Short prose explores a dog's attempts to understand the practices and mysteries of its world.",
                    ["Short Story Collection", "Philosophical Fiction", "Absurdist Fiction", "Classic Literature"],
                    null)
            ]),
        new(
            "Arthur Conan",
            "Doyle",
            "Scottish",
            new DateTime(1859, 5, 22),
            "arthur-conan-doyle.jpg",
            [
                new(
                    "9780192837653",
                    "A Study in Scarlet",
                    "Holmes and Watson investigate a London murder connected to events in the American West.",
                    ["Novel", "Detective Fiction", "Mystery", "Crime Fiction", "Classic Literature"],
                    "doyle-study-scarlet.jpg"),
                new(
                    "9780141395494",
                    "The Sign of the Four",
                    "Holmes and Watson investigate Mary Morstan's missing father, a stolen treasure, and a murder.",
                    ["Novel", "Detective Fiction", "Mystery", "Crime Fiction", "Classic Literature"],
                    null),
                new(
                    "9780008182229",
                    "The Adventures of Sherlock Holmes",
                    "Twelve cases show Holmes solving mysteries involving crime, deception, and unusual evidence.",
                    ["Short Story Collection", "Detective Fiction", "Mystery", "Crime Fiction", "Classic Literature"],
                    "doyle-adventures.jpg"),
                new(
                    "9780007513093",
                    "The Hound of the Baskervilles",
                    "Holmes and Watson investigate a death and a frightening family legend on Dartmoor.",
                    ["Novel", "Detective Fiction", "Mystery", "Gothic Fiction", "Classic Literature"],
                    "doyle-hound-baskervilles.jpg"),
                new(
                    "9781853260582",
                    "The Return of Sherlock Holmes",
                    "A collection of cases follows Holmes's return after he was presumed dead.",
                    ["Short Story Collection", "Detective Fiction", "Mystery", "Crime Fiction", "Classic Literature"],
                    null),
                new(
                    "9780486844596",
                    "The Valley of Fear",
                    "Holmes traces a murder from an English country house to a secretive group in America.",
                    ["Novel", "Detective Fiction", "Mystery", "Crime Fiction", "Classic Literature"],
                    "doyle-valley-fear.jpg"),
                new(
                    "9781306333993",
                    "The Lost World",
                    "An expedition to an isolated plateau encounters prehistoric animals and a dangerous new world.",
                    ["Novel", "Adventure", "Science Fiction", "Classic Literature"],
                    "doyle-lost-world.jpg")
            ]),
        new(
            "Fyodor",
            "Dostoevsky",
            "Russian",
            new DateTime(1821, 11, 11),
            "fyodor-dostoevsky.jpg",
            [
                new(
                    "9780141192802",
                    "Crime and Punishment",
                    "A St. Petersburg student commits a murder and wrestles with guilt, rationalization, and their consequences.",
                    ["Novel", "Psychological Fiction", "Crime Fiction", "Philosophical Fiction", "Classic Literature"],
                    "dostoevsky-crime-punishment.jpg"),
                new(
                    "9780374528379",
                    "The Brothers Karamazov",
                    "A father's murder and his sons' rivalries frame questions of faith, responsibility, and moral freedom.",
                    ["Novel", "Philosophical Fiction", "Family Saga", "Religious Fiction", "Classic Literature"],
                    "dostoevsky-brothers-karamazov.jpg"),
                new(
                    "9780375702242",
                    "The Idiot",
                    "The compassionate Prince Myshkin returns to Russian society, where idealism meets intrigue and desire.",
                    ["Novel", "Psychological Fiction", "Social Satire", "Classic Literature"],
                    "dostoevsky-idiot.jpg"),
                new(
                    "9780679734512",
                    "Demons",
                    "A provincial political circle spirals into conspiracy and exposes ideological conflict and instability.",
                    ["Novel", "Political Fiction", "Psychological Fiction", "Political Satire", "Classic Literature"],
                    "dostoevsky-demons.jpg"),
                new(
                    "9780140455120",
                    "Notes from Underground and the Double",
                    "This combined volume pairs an alienated philosophical narrator with a work about identity and self-division.",
                    ["Novel", "Philosophical Fiction", "Psychological Fiction", "Social Satire", "Classic Literature"],
                    null),
                new(
                    "9780140455090",
                    "The Gambler and Other Stories",
                    "The title novella follows roulette obsession and debt, alongside a collection of shorter fiction.",
                    ["Short Story Collection", "Psychological Fiction", "Gambling Fiction", "Classic Literature"],
                    "dostoevsky-gambler.jpg"),
                new(
                    "9780486434094",
                    "The House of the Dead",
                    "A semi-autobiographical account depicts Siberian penal life through prisoners' experiences and routines.",
                    ["Novel", "Prison Literature", "Memoir", "Historical Fiction", "Classic Literature"],
                    "dostoevsky-house-dead.jpg")
            ])
    ];
}
