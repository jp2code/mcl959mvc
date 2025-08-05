using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace mcl959mvc.Classes;

public class Country {

    public Country() {
        Name = string.Empty;
        Alpha2Code = string.Empty;
        Alpha3Code = string.Empty;
        NumericCode = string.Empty;
        Enabled = false;
    }

    public Country(string name, string alpha2Code, string alpha3Code, string numericCode, bool enabled) {
        Name = name;
        Alpha2Code = alpha2Code;
        Alpha3Code = alpha3Code;
        NumericCode = numericCode;
        Enabled = enabled;
    }

    public string Name { get; set; }
    public string Alpha2Code { get; set; }
    public string Alpha3Code { get; set; }
    public string NumericCode { get; set; }
    public bool Enabled { get; set; }

    public override string ToString() {
        //Returns "USA - United States"
        return string.Format("{0} - {1}", Alpha3Code, Name);
    }
}

public static class CountryArray {
    private static List<Country> m_countries;
    static CountryArray() {
        m_countries = new List<Country>(50);
        m_countries.Add(new Country("Afghanistan", "AF", "AFG", "004", false));
        m_countries.Add(new Country("Aland Islands", "AX", "ALA", "248", false));
        m_countries.Add(new Country("Albania", "AL", "ALB", "008", false));
        m_countries.Add(new Country("Algeria", "DZ", "DZA", "012", false));
        m_countries.Add(new Country("American Samoa", "AS", "ASM", "016", false));
        m_countries.Add(new Country("Andorra", "AD", "AND", "020", false));
        m_countries.Add(new Country("Angola", "AO", "AGO", "024", false));
        m_countries.Add(new Country("Anguilla", "AI", "AIA", "660", false));
        m_countries.Add(new Country("Antarctica", "AQ", "ATA", "010", false));
        m_countries.Add(new Country("Antigua and Barbuda", "AG", "ATG", "028", false));
        m_countries.Add(new Country("Argentina", "AR", "ARG", "032", false));
        m_countries.Add(new Country("Armenia", "AM", "ARM", "051", false));
        m_countries.Add(new Country("Aruba", "AW", "ABW", "533", false));
        m_countries.Add(new Country("Australia", "AU", "AUS", "036", false));
        m_countries.Add(new Country("Austria", "AT", "AUT", "040", false));
        m_countries.Add(new Country("Azerbaijan", "AZ", "AZE", "031", false));
        m_countries.Add(new Country("Bahamas", "BS", "BHS", "044", false));
        m_countries.Add(new Country("Bahrain", "BH", "BHR", "048", false));
        m_countries.Add(new Country("Bangladesh", "BD", "BGD", "050", false));
        m_countries.Add(new Country("Barbados", "BB", "BRB", "052", false));
        m_countries.Add(new Country("Belarus", "BY", "BLR", "112", false));
        m_countries.Add(new Country("Belgium", "BE", "BEL", "056", false));
        m_countries.Add(new Country("Belize", "BZ", "BLZ", "084", false));
        m_countries.Add(new Country("Benin", "BJ", "BEN", "204", false));
        m_countries.Add(new Country("Bermuda", "BM", "BMU", "060", false));
        m_countries.Add(new Country("Bhutan", "BT", "BTN", "064", false));
        m_countries.Add(new Country("Bolivia, Plurinational State of", "BO", "BOL", "068", false));
        m_countries.Add(new Country("Bonaire, Sint Eustatius and Saba", "BQ", "BES", "535", false));
        m_countries.Add(new Country("Bosnia and Herzegovina", "BA", "BIH", "070", false));
        m_countries.Add(new Country("Botswana", "BW", "BWA", "072", false));
        m_countries.Add(new Country("Bouvet Island", "BV", "BVT", "074", false));
        m_countries.Add(new Country("Brazil", "BR", "BRA", "076", false));
        m_countries.Add(new Country("British Indian Ocean Territory", "IO", "IOT", "086", false));
        m_countries.Add(new Country("Brunei Darussalam", "BN", "BRN", "096", false));
        m_countries.Add(new Country("Bulgaria", "BG", "BGR", "100", false));
        m_countries.Add(new Country("Burkina Faso", "BF", "BFA", "854", false));
        m_countries.Add(new Country("Burundi", "BI", "BDI", "108", false));
        m_countries.Add(new Country("Cambodia", "KH", "KHM", "116", false));
        m_countries.Add(new Country("Cameroon", "CM", "CMR", "120", false));
        m_countries.Add(new Country("Canada", "CA", "CAN", "124", true));
        m_countries.Add(new Country("Cape Verde", "CV", "CPV", "132", false));
        m_countries.Add(new Country("Cayman Islands", "KY", "CYM", "136", false));
        m_countries.Add(new Country("Central African Republic", "CF", "CAF", "140", false));
        m_countries.Add(new Country("Chad", "TD", "TCD", "148", false));
        m_countries.Add(new Country("Chile", "CL", "CHL", "152", false));
        m_countries.Add(new Country("China", "CN", "CHN", "156", false));
        m_countries.Add(new Country("Christmas Island", "CX", "CXR", "162", false));
        m_countries.Add(new Country("Cocos (Keeling) Islands", "CC", "CCK", "166", false));
        m_countries.Add(new Country("Colombia", "CO", "COL", "170", false));
        m_countries.Add(new Country("Comoros", "KM", "COM", "174", false));
        m_countries.Add(new Country("Congo", "CG", "COG", "178", false));
        m_countries.Add(new Country("Congo, the Democratic Republic of the", "CD", "COD", "180", false));
        m_countries.Add(new Country("Cook Islands", "CK", "COK", "184", false));
        m_countries.Add(new Country("Costa Rica", "CR", "CRI", "188", false));
        m_countries.Add(new Country("Cote d'Ivoire", "CI", "CIV", "384", false));
        m_countries.Add(new Country("Croatia", "HR", "HRV", "191", false));
        m_countries.Add(new Country("Cuba", "CU", "CUB", "192", false));
        m_countries.Add(new Country("Curacao", "CW", "CUW", "531", false));
        m_countries.Add(new Country("Cyprus", "CY", "CYP", "196", false));
        m_countries.Add(new Country("Czech Republic", "CZ", "CZE", "203", false));
        m_countries.Add(new Country("Denmark", "DK", "DNK", "208", false));
        m_countries.Add(new Country("Djibouti", "DJ", "DJI", "262", false));
        m_countries.Add(new Country("Dominica", "DM", "DMA", "212", false));
        m_countries.Add(new Country("Dominican Republic", "DO", "DOM", "214", false));
        m_countries.Add(new Country("Ecuador", "EC", "ECU", "218", false));
        m_countries.Add(new Country("Egypt", "EG", "EGY", "818", false));
        m_countries.Add(new Country("El Salvador", "SV", "SLV", "222", false));
        m_countries.Add(new Country("Equatorial Guinea", "GQ", "GNQ", "226", false));
        m_countries.Add(new Country("Eritrea", "ER", "ERI", "232", false));
        m_countries.Add(new Country("Estonia", "EE", "EST", "233", false));
        m_countries.Add(new Country("Ethiopia", "ET", "ETH", "231", false));
        m_countries.Add(new Country("Falkland Islands (Malvinas)", "FK", "FLK", "238", false));
        m_countries.Add(new Country("Faroe Islands", "FO", "FRO", "234", false));
        m_countries.Add(new Country("Fiji", "FJ", "FJI", "242", false));
        m_countries.Add(new Country("Finland", "FI", "FIN", "246", false));
        m_countries.Add(new Country("France", "FR", "FRA", "250", false));
        m_countries.Add(new Country("French Guiana", "GF", "GUF", "254", false));
        m_countries.Add(new Country("French Polynesia", "PF", "PYF", "258", false));
        m_countries.Add(new Country("French Southern Territories", "TF", "ATF", "260", false));
        m_countries.Add(new Country("Gabon", "GA", "GAB", "266", false));
        m_countries.Add(new Country("Gambia", "GM", "GMB", "270", false));
        m_countries.Add(new Country("Georgia", "GE", "GEO", "268", false));
        m_countries.Add(new Country("Germany", "DE", "DEU", "276", false));
        m_countries.Add(new Country("Ghana", "GH", "GHA", "288", false));
        m_countries.Add(new Country("Gibraltar", "GI", "GIB", "292", false));
        m_countries.Add(new Country("Greece", "GR", "GRC", "300", false));
        m_countries.Add(new Country("Greenland", "GL", "GRL", "304", false));
        m_countries.Add(new Country("Grenada", "GD", "GRD", "308", false));
        m_countries.Add(new Country("Guadeloupe", "GP", "GLP", "312", false));
        m_countries.Add(new Country("Guam", "GU", "GUM", "316", false));
        m_countries.Add(new Country("Guatemala", "GT", "GTM", "320", false));
        m_countries.Add(new Country("Guernsey", "GG", "GGY", "831", false));
        m_countries.Add(new Country("Guinea", "GN", "GIN", "324", false));
        m_countries.Add(new Country("Guinea-Bissau", "GW", "GNB", "624", false));
        m_countries.Add(new Country("Guyana", "GY", "GUY", "328", false));
        m_countries.Add(new Country("Haiti", "HT", "HTI", "332", false));
        m_countries.Add(new Country("Heard Island and McDonald Islands", "HM", "HMD", "334", false));
        m_countries.Add(new Country("Holy See (Vatican City State)", "VA", "VAT", "336", false));
        m_countries.Add(new Country("Honduras", "HN", "HND", "340", false));
        m_countries.Add(new Country("Hong Kong", "HK", "HKG", "344", false));
        m_countries.Add(new Country("Hungary", "HU", "HUN", "348", false));
        m_countries.Add(new Country("Iceland", "IS", "ISL", "352", false));
        m_countries.Add(new Country("India", "IN", "IND", "356", false));
        m_countries.Add(new Country("Indonesia", "ID", "IDN", "360", false));
        m_countries.Add(new Country("Iran, Islamic Republic of", "IR", "IRN", "364", false));
        m_countries.Add(new Country("Iraq", "IQ", "IRQ", "368", false));
        m_countries.Add(new Country("Ireland", "IE", "IRL", "372", false));
        m_countries.Add(new Country("Isle of Man", "IM", "IMN", "833", false));
        m_countries.Add(new Country("Israel", "IL", "ISR", "376", false));
        m_countries.Add(new Country("Italy", "IT", "ITA", "380", false));
        m_countries.Add(new Country("Jamaica", "JM", "JAM", "388", false));
        m_countries.Add(new Country("Japan", "JP", "JPN", "392", false));
        m_countries.Add(new Country("Jersey", "JE", "JEY", "832", false));
        m_countries.Add(new Country("Jordan", "JO", "JOR", "400", false));
        m_countries.Add(new Country("Kazakhstan", "KZ", "KAZ", "398", false));
        m_countries.Add(new Country("Kenya", "KE", "KEN", "404", false));
        m_countries.Add(new Country("Kiribati", "KI", "KIR", "296", false));
        m_countries.Add(new Country("Korea, Democratic People's Republic of", "KP", "PRK", "408", false));
        m_countries.Add(new Country("Korea, Republic of", "KR", "KOR", "410", false));
        m_countries.Add(new Country("Kuwait", "KW", "KWT", "414", false));
        m_countries.Add(new Country("Kyrgyzstan", "KG", "KGZ", "417", false));
        m_countries.Add(new Country("Lao People's Democratic Republic", "LA", "LAO", "418", false));
        m_countries.Add(new Country("Latvia", "LV", "LVA", "428", false));
        m_countries.Add(new Country("Lebanon", "LB", "LBN", "422", false));
        m_countries.Add(new Country("Lesotho", "LS", "LSO", "426", false));
        m_countries.Add(new Country("Liberia", "LR", "LBR", "430", false));
        m_countries.Add(new Country("Libya", "LY", "LBY", "434", false));
        m_countries.Add(new Country("Liechtenstein", "LI", "LIE", "438", false));
        m_countries.Add(new Country("Lithuania", "LT", "LTU", "440", false));
        m_countries.Add(new Country("Luxembourg", "LU", "LUX", "442", false));
        m_countries.Add(new Country("Macao", "MO", "MAC", "446", false));
        m_countries.Add(new Country("Macedonia, the former Yugoslav Republic of", "MK", "MKD", "807", false));
        m_countries.Add(new Country("Madagascar", "MG", "MDG", "450", false));
        m_countries.Add(new Country("Malawi", "MW", "MWI", "454", false));
        m_countries.Add(new Country("Malaysia", "MY", "MYS", "458", false));
        m_countries.Add(new Country("Maldives", "MV", "MDV", "462", false));
        m_countries.Add(new Country("Mali", "ML", "MLI", "466", false));
        m_countries.Add(new Country("Malta", "MT", "MLT", "470", false));
        m_countries.Add(new Country("Marshall Islands", "MH", "MHL", "584", false));
        m_countries.Add(new Country("Martinique", "MQ", "MTQ", "474", false));
        m_countries.Add(new Country("Mauritania", "MR", "MRT", "478", false));
        m_countries.Add(new Country("Mauritius", "MU", "MUS", "480", false));
        m_countries.Add(new Country("Mayotte", "YT", "MYT", "175", false));
        m_countries.Add(new Country("Mexico", "MX", "MEX", "484", false));
        m_countries.Add(new Country("Micronesia, Federated States of", "FM", "FSM", "583", false));
        m_countries.Add(new Country("Moldova, Republic of", "MD", "MDA", "498", false));
        m_countries.Add(new Country("Monaco", "MC", "MCO", "492", false));
        m_countries.Add(new Country("Mongolia", "MN", "MNG", "496", false));
        m_countries.Add(new Country("Montenegro", "ME", "MNE", "499", false));
        m_countries.Add(new Country("Montserrat", "MS", "MSR", "500", false));
        m_countries.Add(new Country("Morocco", "MA", "MAR", "504", false));
        m_countries.Add(new Country("Mozambique", "MZ", "MOZ", "508", false));
        m_countries.Add(new Country("Myanmar", "MM", "MMR", "104", false));
        m_countries.Add(new Country("Namibia", "NA", "NAM", "516", false));
        m_countries.Add(new Country("Nauru", "NR", "NRU", "520", false));
        m_countries.Add(new Country("Nepal", "NP", "NPL", "524", false));
        m_countries.Add(new Country("Netherlands", "NL", "NLD", "528", false));
        m_countries.Add(new Country("New Caledonia", "NC", "NCL", "540", false));
        m_countries.Add(new Country("New Zealand", "NZ", "NZL", "554", false));
        m_countries.Add(new Country("Nicaragua", "NI", "NIC", "558", false));
        m_countries.Add(new Country("Niger", "NE", "NER", "562", false));
        m_countries.Add(new Country("Nigeria", "NG", "NGA", "566", false));
        m_countries.Add(new Country("Niue", "NU", "NIU", "570", false));
        m_countries.Add(new Country("Norfolk Island", "NF", "NFK", "574", false));
        m_countries.Add(new Country("Northern Mariana Islands", "MP", "MNP", "580", false));
        m_countries.Add(new Country("Norway", "NO", "NOR", "578", false));
        m_countries.Add(new Country("Oman", "OM", "OMN", "512", false));
        m_countries.Add(new Country("Pakistan", "PK", "PAK", "586", false));
        m_countries.Add(new Country("Palau", "PW", "PLW", "585", false));
        m_countries.Add(new Country("Palestine, State of", "PS", "PSE", "275", false));
        m_countries.Add(new Country("Panama", "PA", "PAN", "591", false));
        m_countries.Add(new Country("Papua New Guinea", "PG", "PNG", "598", false));
        m_countries.Add(new Country("Paraguay", "PY", "PRY", "600", false));
        m_countries.Add(new Country("Peru", "PE", "PER", "604", false));
        m_countries.Add(new Country("Philippines", "PH", "PHL", "608", false));
        m_countries.Add(new Country("Pitcairn", "PN", "PCN", "612", false));
        m_countries.Add(new Country("Poland", "PL", "POL", "616", false));
        m_countries.Add(new Country("Portugal", "PT", "PRT", "620", false));
        m_countries.Add(new Country("Puerto Rico", "PR", "PRI", "630", false));
        m_countries.Add(new Country("Qatar", "QA", "QAT", "634", false));
        m_countries.Add(new Country("Reunion", "RE", "REU", "638", false));
        m_countries.Add(new Country("Romania", "RO", "ROU", "642", false));
        m_countries.Add(new Country("Russian Federation", "RU", "RUS", "643", false));
        m_countries.Add(new Country("Rwanda", "RW", "RWA", "646", false));
        m_countries.Add(new Country("Saint BarthÃ©lemy", "BL", "BLM", "652", false));
        m_countries.Add(new Country("Saint Helena, Ascension and Tristan da Cunha", "SH", "SHN", "654", false));
        m_countries.Add(new Country("Saint Kitts and Nevis", "KN", "KNA", "659", false));
        m_countries.Add(new Country("Saint Lucia", "LC", "LCA", "662", false));
        m_countries.Add(new Country("Saint Martin (French part)", "MF", "MAF", "663", false));
        m_countries.Add(new Country("Saint Pierre and Miquelon", "PM", "SPM", "666", false));
        m_countries.Add(new Country("Saint Vincent and the Grenadines", "VC", "VCT", "670", false));
        m_countries.Add(new Country("Samoa", "WS", "WSM", "882", false));
        m_countries.Add(new Country("San Marino", "SM", "SMR", "674", false));
        m_countries.Add(new Country("Sao Tome and Principe", "ST", "STP", "678", false));
        m_countries.Add(new Country("Saudi Arabia", "SA", "SAU", "682", false));
        m_countries.Add(new Country("Senegal", "SN", "SEN", "686", false));
        m_countries.Add(new Country("Serbia", "RS", "SRB", "688", false));
        m_countries.Add(new Country("Seychelles", "SC", "SYC", "690", false));
        m_countries.Add(new Country("Sierra Leone", "SL", "SLE", "694", false));
        m_countries.Add(new Country("Singapore", "SG", "SGP", "702", false));
        m_countries.Add(new Country("Sint Maarten (Dutch part)", "SX", "SXM", "534", false));
        m_countries.Add(new Country("Slovakia", "SK", "SVK", "703", false));
        m_countries.Add(new Country("Slovenia", "SI", "SVN", "705", false));
        m_countries.Add(new Country("Solomon Islands", "SB", "SLB", "090", false));
        m_countries.Add(new Country("Somalia", "SO", "SOM", "706", false));
        m_countries.Add(new Country("South Africa", "ZA", "ZAF", "710", false));
        m_countries.Add(new Country("South Georgia and the South Sandwich Islands", "GS", "SGS", "239", false));
        m_countries.Add(new Country("South Sudan", "SS", "SSD", "728", false));
        m_countries.Add(new Country("Spain", "ES", "ESP", "724", false));
        m_countries.Add(new Country("Sri Lanka", "LK", "LKA", "144", false));
        m_countries.Add(new Country("Sudan", "SD", "SDN", "729", false));
        m_countries.Add(new Country("Suriname", "SR", "SUR", "740", false));
        m_countries.Add(new Country("Svalbard and Jan Mayen", "SJ", "SJM", "744", false));
        m_countries.Add(new Country("Swaziland", "SZ", "SWZ", "748", false));
        m_countries.Add(new Country("Sweden", "SE", "SWE", "752", false));
        m_countries.Add(new Country("Switzerland", "CH", "CHE", "756", false));
        m_countries.Add(new Country("Syrian Arab Republic", "SY", "SYR", "760", false));
        m_countries.Add(new Country("Taiwan, Province of China", "TW", "TWN", "158", false));
        m_countries.Add(new Country("Tajikistan", "TJ", "TJK", "762", false));
        m_countries.Add(new Country("Tanzania, United Republic of", "TZ", "TZA", "834", false));
        m_countries.Add(new Country("Thailand", "TH", "THA", "764", false));
        m_countries.Add(new Country("Timor-Leste", "TL", "TLS", "626", false));
        m_countries.Add(new Country("Togo", "TG", "TGO", "768", false));
        m_countries.Add(new Country("Tokelau", "TK", "TKL", "772", false));
        m_countries.Add(new Country("Tonga", "TO", "TON", "776", false));
        m_countries.Add(new Country("Trinidad and Tobago", "TT", "TTO", "780", false));
        m_countries.Add(new Country("Tunisia", "TN", "TUN", "788", false));
        m_countries.Add(new Country("Turkey", "TR", "TUR", "792", false));
        m_countries.Add(new Country("Turkmenistan", "TM", "TKM", "795", false));
        m_countries.Add(new Country("Turks and Caicos Islands", "TC", "TCA", "796", false));
        m_countries.Add(new Country("Tuvalu", "TV", "TUV", "798", false));
        m_countries.Add(new Country("Uganda", "UG", "UGA", "800", false));
        m_countries.Add(new Country("Ukraine", "UA", "UKR", "804", false));
        m_countries.Add(new Country("United Arab Emirates", "AE", "ARE", "784", false));
        m_countries.Add(new Country("United Kingdom", "GB", "GBR", "826", false));
        m_countries.Add(new Country("United States", "US", "USA", "840", true));
        m_countries.Add(new Country("United States Minor Outlying Islands", "UM", "UMI", "581", false));
        m_countries.Add(new Country("Uruguay", "UY", "URY", "858", false));
        m_countries.Add(new Country("Uzbekistan", "UZ", "UZB", "860", false));
        m_countries.Add(new Country("Vanuatu", "VU", "VUT", "548", false));
        m_countries.Add(new Country("Venezuela, Bolivarian Republic of", "VE", "VEN", "862", false));
        m_countries.Add(new Country("Viet Nam", "VN", "VNM", "704", false));
        m_countries.Add(new Country("Virgin Islands, British", "VG", "VGB", "092", false));
        m_countries.Add(new Country("Virgin Islands, U.S.", "VI", "VIR", "850", false));
        m_countries.Add(new Country("Wallis and Futuna", "WF", "WLF", "876", false));
        m_countries.Add(new Country("Western Sahara", "EH", "ESH", "732", false));
        m_countries.Add(new Country("Yemen", "YE", "YEM", "887", false));
        m_countries.Add(new Country("Zambia", "ZM", "ZMB", "894", false));
        m_countries.Add(new Country("Zimbabwe", "ZW", "ZWE", "716", false));
    }
    /// <summary>
    /// List of 3 digit abbreviated country codes
    /// </summary>
    /// <returns></returns>
    public static string[] Alpha3Codes() {
        var list = new List<string>(m_countries.Count);
        foreach (var country in m_countries.Where(c => c.Enabled)) {
            list.Add(country.Alpha3Code);
        }
        return list.ToArray();
    }
    /// <summary>
    /// List of 2 digit abbreviated country codes
    /// </summary>
    /// <returns></returns>
    public static string[] Alpha2Codes() {
        var list = new List<string>(m_countries.Count);
        foreach (var country in m_countries.Where(c => c.Enabled)) {
            list.Add(country.Alpha2Code);
        }
        return list.ToArray();
    }
    /// <summary>
    /// List of Country names
    /// </summary>
    /// <returns></returns>
    public static string[] Names() {
        var list = new List<string>(m_countries.Count);
        foreach (var country in m_countries.Where(c => c.Enabled)) {
            list.Add(country.Name);
        }
        return list.ToArray();
    }
    /// <summary>
    /// List of Countries
    /// </summary>
    /// <returns></returns>
    public static Country[] Countries() {
        return m_countries.Where(c => c.Enabled == true).ToArray();
    }
}

public class State {

    public State() {
        Name = null;
        Abbreviations = null;
    }

    public State(string ab, string name) {
        Name = name;
        Abbreviations = ab;
    }

    public string Name { get; set; }

    public string Abbreviations { get; set; }

    public override string ToString() {
        return string.Format("{0} - {1}", Abbreviations, Name);
    }

}

public static class StateArray {

    private static List<State> m_states;

    static StateArray() {
        m_states = new List<State>(50);
        m_states.Add(new State("AL", "Alabama"));
        m_states.Add(new State("AK", "Alaska"));
        m_states.Add(new State("AZ", "Arizona"));
        m_states.Add(new State("AR", "Arkansas"));
        m_states.Add(new State("CA", "California"));
        m_states.Add(new State("CO", "Colorado"));
        m_states.Add(new State("CT", "Connecticut"));
        m_states.Add(new State("DE", "Delaware"));
        m_states.Add(new State("DC", "District Of Columbia"));
        m_states.Add(new State("FL", "Florida"));
        m_states.Add(new State("GA", "Georgia"));
        m_states.Add(new State("HI", "Hawaii"));
        m_states.Add(new State("ID", "Idaho"));
        m_states.Add(new State("IL", "Illinois"));
        m_states.Add(new State("IN", "Indiana"));
        m_states.Add(new State("IA", "Iowa"));
        m_states.Add(new State("KS", "Kansas"));
        m_states.Add(new State("KY", "Kentucky"));
        m_states.Add(new State("LA", "Louisiana"));
        m_states.Add(new State("ME", "Maine"));
        m_states.Add(new State("MD", "Maryland"));
        m_states.Add(new State("MA", "Massachusetts"));
        m_states.Add(new State("MI", "Michigan"));
        m_states.Add(new State("MN", "Minnesota"));
        m_states.Add(new State("MS", "Mississippi"));
        m_states.Add(new State("MO", "Missouri"));
        m_states.Add(new State("MT", "Montana"));
        m_states.Add(new State("NE", "Nebraska"));
        m_states.Add(new State("NV", "Nevada"));
        m_states.Add(new State("NH", "New Hampshire"));
        m_states.Add(new State("NJ", "New Jersey"));
        m_states.Add(new State("NM", "New Mexico"));
        m_states.Add(new State("NY", "New York"));
        m_states.Add(new State("NC", "North Carolina"));
        m_states.Add(new State("ND", "North Dakota"));
        m_states.Add(new State("OH", "Ohio"));
        m_states.Add(new State("OK", "Oklahoma"));
        m_states.Add(new State("OR", "Oregon"));
        m_states.Add(new State("PA", "Pennsylvania"));
        m_states.Add(new State("RI", "Rhode Island"));
        m_states.Add(new State("SC", "South Carolina"));
        m_states.Add(new State("SD", "South Dakota"));
        m_states.Add(new State("TN", "Tennessee"));
        m_states.Add(new State("TX", "Texas"));
        m_states.Add(new State("UT", "Utah"));
        m_states.Add(new State("VT", "Vermont"));
        m_states.Add(new State("VA", "Virginia"));
        m_states.Add(new State("WA", "Washington"));
        m_states.Add(new State("WV", "West Virginia"));
        m_states.Add(new State("WI", "Wisconsin"));
        m_states.Add(new State("WY", "Wyoming"));
    }

    public static string[] Abbreviations() {
        var list = new List<string>(m_states.Count);
        foreach (var state in m_states) {
            list.Add(state.Abbreviations);
        }
        return list.ToArray();
    }

    public static string[] Names() {
        var list = new List<string>(m_states.Count);
        foreach (var state in m_states) {
            list.Add(state.Name);
        }
        return list.ToArray();
    }

    public static State[] States() {
        return m_states.ToArray();
    }

}