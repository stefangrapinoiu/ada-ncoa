using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Web.Data;

public static class DbSeeder
{
    public const string DevUserOneEmail = "ioana@example.local";
    public const string DevUserTwoEmail = "andrei@example.local";

    /// <summary>Version 1 activates Romania only; further countries are added here, not in code paths.</summary>
    public static readonly (string Code, string Name)[] Countries =
    {
        ("RO", "România")
    };

    /// <summary>
    /// Romania's 41 județe, plus municipiul București modelled the same way (its own "county"
    /// with a single city — see <see cref="RomanianCities"/>) so the Country → County → City
    /// cascade stays uniform. Car-plate style codes, unique inside the country.
    /// </summary>
    public static readonly (string Code, string Name)[] Counties =
    {
        ("AB", "Alba"),
        ("AR", "Arad"),
        ("AG", "Argeș"),
        ("BC", "Bacău"),
        ("BH", "Bihor"),
        ("BN", "Bistrița-Năsăud"),
        ("BT", "Botoșani"),
        ("BV", "Brașov"),
        ("BR", "Brăila"),
        ("BZ", "Buzău"),
        ("CS", "Caraș-Severin"),
        ("CL", "Călărași"),
        ("CJ", "Cluj"),
        ("CT", "Constanța"),
        ("CV", "Covasna"),
        ("DB", "Dâmbovița"),
        ("DJ", "Dolj"),
        ("GL", "Galați"),
        ("GR", "Giurgiu"),
        ("GJ", "Gorj"),
        ("HR", "Harghita"),
        ("HD", "Hunedoara"),
        ("IL", "Ialomița"),
        ("IS", "Iași"),
        ("IF", "Ilfov"),
        ("MM", "Maramureș"),
        ("MH", "Mehedinți"),
        ("MS", "Mureș"),
        ("NT", "Neamț"),
        ("OT", "Olt"),
        ("PH", "Prahova"),
        ("SJ", "Sălaj"),
        ("SM", "Satu Mare"),
        ("SB", "Sibiu"),
        ("SV", "Suceava"),
        ("TR", "Teleorman"),
        ("TM", "Timiș"),
        ("TL", "Tulcea"),
        ("VL", "Vâlcea"),
        ("VS", "Vaslui"),
        ("VN", "Vrancea"),
        ("B", "București")
    };

    /// <summary>
    /// Every city belongs to a județ (by code, above). Slugs are diacritic-free and unique
    /// inside the country. Each județ's reședință (seat) is listed first; the flagship cities
    /// that already have street-level neighborhoods seeded below (see <see cref="Neighborhoods"/>)
    /// also list a handful of well-known surrounding localities.
    /// </summary>
    public static readonly (string CountyCode, string Slug, string Name)[] RomanianCities =
    {
        ("AB", "alba-iulia", "Alba Iulia"),
        ("AB", "sebes", "Sebeș"),
        ("AB", "aiud", "Aiud"),
        ("AB", "blaj", "Blaj"),
        ("AB", "cugir", "Cugir"),
        ("AB", "ocna-mures", "Ocna Mureș"),
        ("AB", "teius", "Teiuș"),
        ("AB", "abrud", "Abrud"),
        ("AB", "baia-de-aries", "Baia de Arieș"),
        ("AB", "campeni", "Câmpeni"),
        ("AB", "zlatna", "Zlatna"),
        ("AR", "arad", "Arad"),
        ("AR", "lipova", "Lipova"),
        ("AR", "ineu", "Ineu"),
        ("AR", "curtici", "Curtici"),
        ("AR", "chisineu-cris", "Chișineu-Criș"),
        ("AR", "pancota", "Pâncota"),
        ("AR", "nadlac", "Nădlac"),
        ("AR", "sebis", "Sebiș"),
        ("AR", "santana", "Sântana"),
        ("AR", "pecica", "Pecica"),
        ("AG", "pitesti", "Pitești"),
        ("AG", "campulung", "Câmpulung"),
        ("AG", "curtea-de-arges", "Curtea de Argeș"),
        ("AG", "mioveni", "Mioveni"),
        ("AG", "stefanesti", "Ștefănești"),
        ("AG", "costesti", "Costești"),
        ("AG", "topoloveni", "Topoloveni"),
        ("BC", "bacau", "Bacău"),
        ("BC", "onesti", "Onești"),
        ("BC", "moinesti", "Moinești"),
        ("BC", "comanesti", "Comănești"),
        ("BC", "buhusi", "Buhuși"),
        ("BC", "targu-ocna", "Târgu Ocna"),
        ("BC", "slanic-moldova", "Slănic-Moldova"),
        ("BC", "darmanesti", "Dărmănești"),
        ("BH", "oradea", "Oradea"),
        ("BH", "salonta", "Salonta"),
        ("BH", "marghita", "Marghita"),
        ("BH", "beius", "Beiuș"),
        ("BH", "alesd", "Aleșd"),
        ("BH", "valea-lui-mihai", "Valea lui Mihai"),
        ("BH", "sacueni", "Săcueni"),
        ("BH", "stei", "Ștei"),
        ("BH", "vascau", "Vașcău"),
        ("BH", "nucet", "Nucet"),
        ("BH", "santandrei", "Sântandrei"),
        ("BH", "biharia", "Biharia"),
        ("BN", "bistrita", "Bistrița"),
        ("BN", "beclean", "Beclean"),
        ("BN", "nasaud", "Năsăud"),
        ("BN", "sangeorz-bai", "Sângeorz-Băi"),
        ("BT", "botosani", "Botoșani"),
        ("BT", "dorohoi", "Dorohoi"),
        ("BT", "darabani", "Darabani"),
        ("BT", "flamanzi", "Flămânzi"),
        ("BT", "saveni", "Săveni"),
        ("BT", "stefanesti-bt", "Ștefănești"),
        ("BT", "bucecea", "Bucecea"),
        ("BV", "brasov", "Brașov"),
        ("BV", "sacele", "Săcele"),
        ("BV", "fagaras", "Făgăraș"),
        ("BV", "codlea", "Codlea"),
        ("BV", "zarnesti", "Zărnești"),
        ("BV", "rasnov", "Râșnov"),
        ("BV", "predeal", "Predeal"),
        ("BV", "ghimbav", "Ghimbav"),
        ("BV", "rupea", "Rupea"),
        ("BV", "victoria", "Victoria"),
        ("BV", "sanpetru", "Sânpetru"),
        ("BV", "bod", "Bod"),
        ("BV", "harman", "Hărman"),
        ("BV", "prejmer", "Prejmer"),
        ("BV", "cristian", "Cristian"),
        ("BR", "braila", "Brăila"),
        ("BR", "ianca", "Ianca"),
        ("BR", "insuratei", "Însurăței"),
        ("BR", "faurei", "Făurei"),
        ("BZ", "buzau", "Buzău"),
        ("BZ", "ramnicu-sarat", "Râmnicu Sărat"),
        ("BZ", "nehoiu", "Nehoiu"),
        ("BZ", "patarlagele", "Pătârlagele"),
        ("BZ", "pogoanele", "Pogoanele"),
        ("CS", "resita", "Reșița"),
        ("CS", "caransebes", "Caransebeș"),
        ("CS", "oravita", "Oravița"),
        ("CS", "moldova-noua", "Moldova Nouă"),
        ("CS", "bocsa", "Bocșa"),
        ("CS", "otelu-rosu", "Oțelu Roșu"),
        ("CS", "anina", "Anina"),
        ("CS", "baile-herculane", "Băile Herculane"),
        ("CL", "calarasi", "Călărași"),
        ("CL", "oltenita", "Oltenița"),
        ("CL", "lehliu-gara", "Lehliu Gară"),
        ("CL", "budesti", "Budești"),
        ("CL", "fundulea", "Fundulea"),
        ("CJ", "cluj-napoca", "Cluj-Napoca"),
        ("CJ", "turda", "Turda"),
        ("CJ", "dej", "Dej"),
        ("CJ", "campia-turzii", "Câmpia Turzii"),
        ("CJ", "gherla", "Gherla"),
        ("CJ", "huedin", "Huedin"),
        ("CJ", "floresti", "Florești"),
        ("CJ", "apahida", "Apahida"),
        ("CJ", "baciu", "Baciu"),
        ("CJ", "gilau", "Gilău"),
        ("CJ", "feleacu", "Feleacu"),
        ("CJ", "chinteni", "Chinteni"),
        ("CJ", "jucu", "Jucu"),
        ("CT", "constanta", "Constanța"),
        ("CT", "mangalia", "Mangalia"),
        ("CT", "medgidia", "Medgidia"),
        ("CT", "navodari", "Năvodari"),
        ("CT", "cernavoda", "Cernavodă"),
        ("CT", "murfatlar", "Murfatlar"),
        ("CT", "ovidiu", "Ovidiu"),
        ("CT", "harsova", "Hârșova"),
        ("CT", "eforie", "Eforie"),
        ("CT", "techirghiol", "Techirghiol"),
        ("CT", "negru-voda", "Negru Vodă"),
        ("CT", "agigea", "Agigea"),
        ("CT", "corbu", "Corbu"),
        ("CT", "costinesti", "Costinești"),
        ("CT", "valu-lui-traian", "Valu lui Traian"),
        ("CT", "mihail-kogalniceanu", "Mihail Kogălniceanu"),
        ("CV", "sfantu-gheorghe", "Sfântu Gheorghe"),
        ("CV", "targu-secuiesc", "Târgu Secuiesc"),
        ("CV", "covasna", "Covasna"),
        ("CV", "baraolt", "Baraolt"),
        ("CV", "intorsura-buzaului", "Întorsura Buzăului"),
        ("DB", "targoviste", "Târgoviște"),
        ("DB", "moreni", "Moreni"),
        ("DB", "pucioasa", "Pucioasa"),
        ("DB", "gaesti", "Găești"),
        ("DB", "titu", "Titu"),
        ("DB", "racari", "Răcari"),
        ("DB", "fieni", "Fieni"),
        ("DJ", "craiova", "Craiova"),
        ("DJ", "bailesti", "Băilești"),
        ("DJ", "filiasi", "Filiași"),
        ("DJ", "calafat", "Calafat"),
        ("DJ", "segarcea", "Segarcea"),
        ("DJ", "bechet", "Bechet"),
        ("DJ", "dabuleni", "Dăbuleni"),
        ("DJ", "isalnita", "Ișalnița"),
        ("DJ", "podari", "Podari"),
        ("GL", "galati", "Galați"),
        ("GL", "tecuci", "Tecuci"),
        ("GL", "targu-bujor", "Târgu Bujor"),
        ("GL", "beresti", "Berești"),
        ("GR", "giurgiu", "Giurgiu"),
        ("GR", "bolintin-vale", "Bolintin-Vale"),
        ("GR", "mihailesti", "Mihăilești"),
        ("GJ", "targu-jiu", "Târgu Jiu"),
        ("GJ", "motru", "Motru"),
        ("GJ", "rovinari", "Rovinari"),
        ("GJ", "bumbesti-jiu", "Bumbești-Jiu"),
        ("GJ", "targu-carbunesti", "Târgu Cărbunești"),
        ("GJ", "ticleni", "Țicleni"),
        ("GJ", "turceni", "Turceni"),
        ("GJ", "novaci", "Novaci"),
        ("HR", "miercurea-ciuc", "Miercurea Ciuc"),
        ("HR", "odorheiu-secuiesc", "Odorheiu Secuiesc"),
        ("HR", "gheorgheni", "Gheorgheni"),
        ("HR", "toplita", "Toplița"),
        ("HR", "cristuru-secuiesc", "Cristuru Secuiesc"),
        ("HR", "balan", "Bălan"),
        ("HR", "borsec", "Borsec"),
        ("HR", "baile-tusnad", "Băile Tușnad"),
        ("HR", "vlahita", "Vlăhița"),
        ("HD", "deva", "Deva"),
        ("HD", "hunedoara", "Hunedoara"),
        ("HD", "petrosani", "Petroșani"),
        ("HD", "vulcan", "Vulcan"),
        ("HD", "petrila", "Petrila"),
        ("HD", "orastie", "Orăștie"),
        ("HD", "brad", "Brad"),
        ("HD", "calan", "Călan"),
        ("HD", "simeria", "Simeria"),
        ("HD", "hateg", "Hațeg"),
        ("HD", "geoagiu", "Geoagiu"),
        ("HD", "aninoasa", "Aninoasa"),
        ("HD", "uricani", "Uricani"),
        ("IL", "slobozia", "Slobozia"),
        ("IL", "fetesti", "Fetești"),
        ("IL", "urziceni", "Urziceni"),
        ("IL", "tandarei", "Țăndărei"),
        ("IL", "amara", "Amara"),
        ("IL", "fierbinti-targ", "Fierbinți-Târg"),
        ("IS", "iasi", "Iași"),
        ("IS", "pascani", "Pașcani"),
        ("IS", "harlau", "Hârlău"),
        ("IS", "podu-iloaiei", "Podu Iloaiei"),
        ("IS", "targu-frumos", "Târgu Frumos"),
        ("IS", "miroslava", "Miroslava"),
        ("IS", "ciurea", "Ciurea"),
        ("IS", "holboca", "Holboca"),
        ("IS", "valea-lupului", "Valea Lupului"),
        ("IS", "tomesti", "Tomești"),
        ("IF", "buftea", "Buftea"),
        ("IF", "voluntari", "Voluntari"),
        ("IF", "popesti-leordeni", "Popești-Leordeni"),
        ("IF", "bragadiru", "Bragadiru"),
        ("IF", "pantelimon", "Pantelimon"),
        ("IF", "otopeni", "Otopeni"),
        ("IF", "chitila", "Chitila"),
        ("IF", "magurele", "Măgurele"),
        ("IF", "chiajna", "Chiajna"),
        ("IF", "mogosoaia", "Mogoșoaia"),
        ("IF", "corbeanca", "Corbeanca"),
        ("MM", "baia-mare", "Baia Mare"),
        ("MM", "sighetu-marmatiei", "Sighetu Marmației"),
        ("MM", "borsa", "Borșa"),
        ("MM", "viseu-de-sus", "Vișeu de Sus"),
        ("MM", "targu-lapus", "Târgu Lăpuș"),
        ("MM", "cavnic", "Cavnic"),
        ("MM", "seini", "Seini"),
        ("MM", "ulmeni", "Ulmeni"),
        ("MM", "somcuta-mare", "Șomcuta Mare"),
        ("MM", "dragomiresti", "Dragomirești"),
        ("MM", "tautii-magheraus", "Tăuții-Măgherăuș"),
        ("MH", "drobeta-turnu-severin", "Drobeta-Turnu Severin"),
        ("MH", "strehaia", "Strehaia"),
        ("MH", "orsova", "Orșova"),
        ("MH", "vanju-mare", "Vânju Mare"),
        ("MH", "baia-de-arama", "Baia de Aramă"),
        ("MS", "targu-mures", "Târgu Mureș"),
        ("MS", "reghin", "Reghin"),
        ("MS", "sighisoara", "Sighișoara"),
        ("MS", "ludus", "Luduș"),
        ("MS", "sovata", "Sovata"),
        ("MS", "tarnaveni", "Târnăveni"),
        ("MS", "ungheni", "Ungheni"),
        ("MS", "iernut", "Iernut"),
        ("MS", "sangeorgiu-de-padure", "Sângeorgiu de Pădure"),
        ("MS", "miercurea-nirajului", "Miercurea Nirajului"),
        ("MS", "sarmasu", "Sărmașu"),
        ("MS", "sangeorgiu-de-mures", "Sângeorgiu de Mureș"),
        ("MS", "corunca", "Corunca"),
        ("NT", "piatra-neamt", "Piatra Neamț"),
        ("NT", "roman", "Roman"),
        ("NT", "targu-neamt", "Târgu Neamț"),
        ("NT", "roznov", "Roznov"),
        ("NT", "bicaz", "Bicaz"),
        ("OT", "slatina", "Slatina"),
        ("OT", "caracal", "Caracal"),
        ("OT", "corabia", "Corabia"),
        ("OT", "scornicesti", "Scornicești"),
        ("OT", "bals", "Balș"),
        ("OT", "draganesti-olt", "Drăgănești-Olt"),
        ("OT", "potcoava", "Potcoava"),
        ("OT", "piatra-olt", "Piatra-Olt"),
        ("PH", "ploiesti", "Ploiești"),
        ("PH", "campina", "Câmpina"),
        ("PH", "baicoi", "Băicoi"),
        ("PH", "mizil", "Mizil"),
        ("PH", "valenii-de-munte", "Vălenii de Munte"),
        ("PH", "breaza", "Breaza"),
        ("PH", "comarnic", "Comarnic"),
        ("PH", "boldesti-scaeni", "Boldești-Scăeni"),
        ("PH", "urlati", "Urlați"),
        ("PH", "plopeni", "Plopeni"),
        ("PH", "sinaia", "Sinaia"),
        ("PH", "busteni", "Bușteni"),
        ("PH", "azuga", "Azuga"),
        ("PH", "slanic", "Slănic"),
        ("SJ", "zalau", "Zalău"),
        ("SJ", "simleu-silvaniei", "Șimleu Silvaniei"),
        ("SJ", "jibou", "Jibou"),
        ("SJ", "cehu-silvaniei", "Cehu Silvaniei"),
        ("SM", "satu-mare", "Satu Mare"),
        ("SM", "carei", "Carei"),
        ("SM", "negresti-oas", "Negrești-Oaș"),
        ("SM", "tasnad", "Tășnad"),
        ("SM", "livada", "Livada"),
        ("SM", "ardud", "Ardud"),
        ("SB", "sibiu", "Sibiu"),
        ("SB", "medias", "Mediaș"),
        ("SB", "cisnadie", "Cisnădie"),
        ("SB", "avrig", "Avrig"),
        ("SB", "talmaciu", "Tălmaciu"),
        ("SB", "dumbraveni", "Dumbrăveni"),
        ("SB", "saliste", "Săliște"),
        ("SB", "copsa-mica", "Copșa Mică"),
        ("SB", "agnita", "Agnita"),
        ("SB", "miercurea-sibiului", "Miercurea Sibiului"),
        ("SB", "ocna-sibiului", "Ocna Sibiului"),
        ("SB", "selimbar", "Șelimbăr"),
        ("SB", "cristian-sb", "Cristian"),
        ("SV", "suceava", "Suceava"),
        ("SV", "radauti", "Rădăuți"),
        ("SV", "falticeni", "Fălticeni"),
        ("SV", "campulung-moldovenesc", "Câmpulung Moldovenesc"),
        ("SV", "vicovu-de-sus", "Vicovu de Sus"),
        ("SV", "gura-humorului", "Gura Humorului"),
        ("SV", "vatra-dornei", "Vatra Dornei"),
        ("SV", "siret", "Siret"),
        ("SV", "solca", "Solca"),
        ("SV", "brosteni", "Broșteni"),
        ("SV", "frasin", "Frasin"),
        ("SV", "cajvana", "Cajvana"),
        ("SV", "milisauti", "Milișăuți"),
        ("SV", "dolhasca", "Dolhasca"),
        ("SV", "liteni", "Liteni"),
        ("SV", "salcea", "Salcea"),
        ("TR", "alexandria", "Alexandria"),
        ("TR", "rosiorii-de-vede", "Roșiorii de Vede"),
        ("TR", "turnu-magurele", "Turnu Măgurele"),
        ("TR", "zimnicea", "Zimnicea"),
        ("TR", "videle", "Videle"),
        ("TM", "timisoara", "Timișoara"),
        ("TM", "lugoj", "Lugoj"),
        ("TM", "sannicolau-mare", "Sânnicolau Mare"),
        ("TM", "jimbolia", "Jimbolia"),
        ("TM", "buzias", "Buziaș"),
        ("TM", "faget", "Făget"),
        ("TM", "deta", "Deta"),
        ("TM", "gataia", "Gătaia"),
        ("TM", "ciacova", "Ciacova"),
        ("TM", "dumbravita", "Dumbrăvița"),
        ("TM", "giroc", "Giroc"),
        ("TM", "mosnita-noua", "Moșnița Nouă"),
        ("TM", "ghiroda", "Ghiroda"),
        ("TL", "tulcea", "Tulcea"),
        ("TL", "babadag", "Babadag"),
        ("TL", "macin", "Măcin"),
        ("TL", "isaccea", "Isaccea"),
        ("TL", "sulina", "Sulina"),
        ("VL", "ramnicu-valcea", "Râmnicu Vâlcea"),
        ("VL", "dragasani", "Drăgășani"),
        ("VL", "babeni", "Băbeni"),
        ("VL", "calimanesti", "Călimănești"),
        ("VL", "brezoi", "Brezoi"),
        ("VL", "horezu", "Horezu"),
        ("VL", "baile-olanesti", "Băile Olănești"),
        ("VL", "baile-govora", "Băile Govora"),
        ("VL", "ocnele-mari", "Ocnele Mari"),
        ("VL", "berbesti", "Berbești"),
        ("VL", "balcesti", "Bălcești"),
        ("VS", "vaslui", "Vaslui"),
        ("VS", "barlad", "Bârlad"),
        ("VS", "husi", "Huși"),
        ("VS", "negresti", "Negrești"),
        ("VS", "murgeni", "Murgeni"),
        ("VN", "focsani", "Focșani"),
        ("VN", "adjud", "Adjud"),
        ("VN", "odobesti", "Odobești"),
        ("VN", "panciu", "Panciu"),
        ("VN", "marasesti", "Mărășești"),
        ("B", "bucuresti", "București")
    };

    /// <summary>
    /// Neighborhoods are optional everywhere. A city with no rows here simply shows no
    /// neighborhood filter — nothing else in the application changes.
    /// </summary>
    public static readonly (string CitySlug, string[] Names)[] Neighborhoods =
    {
        ("cluj-napoca", new[]
        {
            "Borhanci", "Bună Ziua", "Centru", "Gheorgheni", "Grigorescu",
            "Iris", "Mănăștur", "Mărăști", "Zorilor"
        }),
        ("bucuresti", new[]
        {
            "Sector 1", "Sector 2", "Sector 3", "Sector 4", "Sector 5", "Sector 6"
        }),
        ("timisoara", new[] { "Centru", "Complex Studențesc", "Fabric", "Girocului", "Iosefin", "Lipovei" }),
        ("iasi", new[] { "Centru", "Copou", "Nicolina", "Păcurari", "Tătărași" }),
        ("brasov", new[] { "Astra", "Bartolomeu", "Centrul Vechi", "Racadau", "Tractorul" }),
        ("sibiu", new[] { "Centru", "Hipodrom", "Terezian", "Vasile Aaron" }),
        ("oradea", new[] { "Centru", "Iosia", "Nufărul", "Rogerius" }),
        ("constanta", new[] { "Centru", "Faleză Nord", "Mamaia", "Tomis Nord" }),
        ("craiova", new[] { "Brazda lui Novac", "Centru", "Craiovița Nouă", "Rovine" }),
        ("targu-mures", new[] { "Centru", "Dâmbul Pietros", "Tudor Vladimirescu", "Unirii" })
    };

    /// <summary>
    /// The category allowlist. Meat and dairy are seeded as rows with <c>IsAllowed = false</c>
    /// on purpose: the rule is then visible in the data and enforced by the service layer,
    /// rather than being a category that merely does not exist in a dropdown.
    /// </summary>
    public static readonly (string Key, string Name, string? Description, bool IsAllowed)[] FoodCategories =
    {
        ("PackagedBakery", "Panificație ambalată",
            "Pâine, lipii, biscuiți sau produse de panificație ambalate și sigilate.", true),
        ("PackagedProduce", "Fructe și legume ambalate",
            "Fructe și legume în ambalaj original, sigilat, cu termen de valabilitate vizibil.", true),
        ("PackagedPantry", "Produse alimentare vegetale ambalate",
            "Paste, orez, leguminoase, conserve vegetale, ulei, făină, zahăr — toate sigilate.", true),
        ("PackagedSnacks", "Gustări ambalate",
            "Batoane, covrigi, semințe, fructe uscate sau alte gustări sigilate.", true),
        ("PackagedBeverages", "Băuturi nealcoolice ambalate",
            "Apă, sucuri, ceai sau cafea în ambalaj original, sigilat. Fără alcool.", true),
        ("PackagedBabyFood", "Alimente ambalate pentru bebeluși",
            "Piureuri și cereale pentru bebeluși, sigilate, cu termen de valabilitate lung.", true),
        ("OtherApproved", "Alte alimente ambalate aprobate",
            "Doar dacă alimentul nu se încadrează în categoriile de mai sus. Denumește clar produsul.", true),

        // Present but never selectable — see IsAllowed = false.
        ("Meat", "Carne și produse din carne", "Categorie interzisă pe platformă.", false),
        ("Fish", "Pește și fructe de mare", "Categorie interzisă pe platformă.", false),
        ("Dairy", "Lactate", "Categorie interzisă pe platformă.", false),
        ("HomeCooked", "Mâncare gătită în casă", "Categorie interzisă în această versiune.", false),
        ("Alcohol", "Băuturi alcoolice", "Categorie interzisă pe platformă.", false)
    };

    /// <summary>Applies migrations (optional) and seeds reference data and — optionally — development data.</summary>
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));
        var db = provider.GetRequiredService<ApplicationDbContext>();

        if (configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            logger.LogInformation("Applying database migrations");
            await db.Database.MigrateAsync();
        }

        await SeedLocationsAsync(db);
        await SeedFoodCategoriesAsync(db);

        if (configuration.GetValue<bool>("Database:SeedSampleData"))
        {
            var password = configuration["Seed:DevUserPassword"];
            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("Seed:DevUserPassword is not configured; sample users were not created");
                return;
            }

            await SeedSampleDataAsync(db, provider.GetRequiredService<UserManager<ApplicationUser>>(), password, logger);
        }
    }

    public static async Task SeedLocationsAsync(ApplicationDbContext db)
    {
        foreach (var (code, name) in Countries)
        {
            if (!await db.Countries.AnyAsync(c => c.Code == code))
            {
                db.Countries.Add(new Country { Code = code, Name = name, IsActive = true });
            }
        }

        await db.SaveChangesAsync();

        var romania = await db.Countries.FirstAsync(c => c.Code == AppInfo.DefaultCountryCode);

        var existingCounties = await db.Counties
            .Where(c => c.CountryId == romania.Id)
            .Select(c => c.Code)
            .ToListAsync();

        foreach (var (code, name) in Counties)
        {
            if (!existingCounties.Contains(code))
            {
                db.Counties.Add(new County { CountryId = romania.Id, Code = code, Name = name, IsActive = true });
            }
        }

        await db.SaveChangesAsync();

        var countyIds = await db.Counties
            .Where(c => c.CountryId == romania.Id)
            .ToDictionaryAsync(c => c.Code, c => c.Id);

        var existingCities = await db.Cities
            .Where(c => c.CountryId == romania.Id)
            .Select(c => c.Slug)
            .ToListAsync();

        foreach (var (countyCode, slug, name) in RomanianCities)
        {
            if (!existingCities.Contains(slug))
            {
                db.Cities.Add(new City
                {
                    CountryId = romania.Id,
                    CountyId = countyIds[countyCode],
                    Slug = slug,
                    Name = name,
                    IsActive = true
                });
            }
        }

        await db.SaveChangesAsync();

        var cityIds = await db.Cities
            .Where(c => c.CountryId == romania.Id)
            .ToDictionaryAsync(c => c.Slug, c => c.Id);

        var existingNeighborhoods = await db.Neighborhoods
            .Select(n => new { n.CityId, n.Name })
            .ToListAsync();

        var known = existingNeighborhoods.Select(n => (n.CityId, n.Name)).ToHashSet();

        foreach (var (citySlug, names) in Neighborhoods)
        {
            if (!cityIds.TryGetValue(citySlug, out var cityId))
            {
                continue;
            }

            foreach (var name in names)
            {
                if (known.Add((cityId, name)))
                {
                    db.Neighborhoods.Add(new Neighborhood { CityId = cityId, Name = name, IsActive = true });
                }
            }
        }

        await db.SaveChangesAsync();
    }

    public static async Task SeedFoodCategoriesAsync(ApplicationDbContext db)
    {
        var existing = await db.FoodCategories.ToDictionaryAsync(c => c.Key);
        var order = 0;

        foreach (var (key, name, description, isAllowed) in FoodCategories)
        {
            order++;

            if (existing.TryGetValue(key, out var category))
            {
                // Keep the allowlist authoritative: a category that became prohibited must not
                // stay usable just because it already exists in an older database.
                category.Name = name;
                category.Description = description;
                category.IsAllowed = isAllowed;
                category.SortOrder = order;
                continue;
            }

            db.FoodCategories.Add(new FoodCategory
            {
                Key = key,
                Name = name,
                Description = description,
                IsActive = true,
                IsAllowed = isAllowed,
                SortOrder = order
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedSampleDataAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        string password,
        ILogger logger)
    {
        var cluj = await db.Cities.FirstOrDefaultAsync(c => c.Slug == "cluj-napoca");
        if (cluj is null)
        {
            return;
        }

        var marasti = await db.Neighborhoods.FirstOrDefaultAsync(n => n.CityId == cluj.Id && n.Name == "Mărăști");
        var gheorgheni = await db.Neighborhoods.FirstOrDefaultAsync(n => n.CityId == cluj.Id && n.Name == "Gheorgheni");

        var ioana = await EnsureUserAsync(userManager, DevUserOneEmail, "Ioana", "Popescu", "0722000111",
            cluj.CountryId, cluj.Id, marasti?.Id, password, logger);
        await EnsureUserAsync(userManager, DevUserTwoEmail, "Andrei", "Ionescu", "0733000222",
            cluj.CountryId, cluj.Id, gheorgheni?.Id, password, logger);

        if (ioana is null || await db.DonationItems.AnyAsync())
        {
            return;
        }

        var categories = await db.FoodCategories
            .Where(c => c.IsAllowed)
            .ToDictionaryAsync(c => c.Key, c => c.Id);

        var now = DateTime.UtcNow;
        var today = RoDate.Today;

        var samples = new (string Title, string CategoryKey, int ExpiresInDays, int? NeighborhoodId, string PickupLocation, string? PickupNotes)[]
        {
            ("Pâine integrală feliată", "PackagedBakery", 5, marasti?.Id,
                "Stația de tramvai Mărăști", "Zilnic după ora 18:00."),
            ("Paste penne integrale 500 g", "PackagedPantry", 240, marasti?.Id,
                "Intrarea Iulius Mall, dinspre Bulevardul 21 Decembrie", null),
            ("Suc natural de mere 1 l", "PackagedBeverages", 90, gheorgheni?.Id,
                "Parcul Iuliu Hațieganu, intrarea principală", "Weekend, între 10:00 și 14:00."),
            ("Mere ambalate 1 kg", "PackagedProduce", 8, gheorgheni?.Id,
                "Piața Mihai Viteazu, lângă intrare", null),
            ("Batoane cu ovăz și fructe", "PackagedSnacks", 120, null,
                "Piața Unirii, lângă statuia lui Matei Corvin", "Pot lăsa pachetul și la birou, în centru."),
            ("Orez cu bob lung 1 kg", "PackagedPantry", 400, marasti?.Id,
                "Stația de autobuz Aurel Vlaicu", null),
            ("Biscuiți digestivi", "PackagedBakery", 60, null,
                "Gara Cluj-Napoca, intrarea principală", "Doar în timpul săptămânii, dimineața.")
        };

        var index = 0;
        foreach (var (title, categoryKey, expiresInDays, neighborhoodId, pickupLocation, pickupNotes) in samples)
        {
            if (!categories.TryGetValue(categoryKey, out var categoryId))
            {
                continue;
            }

            db.DonationItems.Add(new DonationItem
            {
                Title = title,
                FoodCategoryId = categoryId,
                ExpirationDate = today.AddDays(expiresInDays),
                CountryId = cluj.CountryId,
                CityId = cluj.Id,
                NeighborhoodId = neighborhoodId,
                PickupLocation = pickupLocation,
                PickupNotes = pickupNotes,
                Status = DonationStatus.Available,
                DonatorId = ioana.Id,
                SafetyConfirmedAt = now,
                CreatedAt = now.AddHours(-(++index * 5))
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Sample food donations seeded");
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string firstName,
        string lastName,
        string phone,
        int countryId,
        int cityId,
        int? neighborhoodId,
        string password,
        ILogger logger)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return user;
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phone,
            PreferredCountryId = countryId,
            PreferredCityId = cityId,
            PreferredNeighborhoodId = neighborhoodId,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            return user;
        }

        logger.LogWarning("Could not create sample user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Code)));
        return null;
    }
}
