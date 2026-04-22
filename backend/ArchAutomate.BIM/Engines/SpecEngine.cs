using ArchAutomate.BIM.Models;
using System.Text.RegularExpressions;

namespace ArchAutomate.BIM.Engines;

/// <summary>
/// Compiles a text-based construction specification from IFC-detected material keywords.
/// Clause content is based on South African SANS standards and industry conventions.
/// </summary>
public class SpecEngine
{
    // ── Spec clause library ──────────────────────────────────────────────────
    // Each entry: keyword patterns → SpecClause template.
    // Patterns are matched case-insensitively against tokenised material strings.
    private static readonly List<SpecRule> Rules =
    [
        // ── Substructure ────────────────────────────────────────────────────
        new(
            ["concrete", "cast-in-situ", "in-situ", "insitu", "rcc", "reinforced"],
            section: "Substructure",
            heading: "In-situ Reinforced Concrete",
            text: """
                All in-situ concrete to comply with SANS 10100-1 (The structural use of concrete).
                Minimum characteristic compressive strength: 25 MPa at 28 days for foundations and slabs,
                unless otherwise specified by the structural engineer. Concrete mix design to be submitted
                for approval prior to pouring. Ready-mix supplier to hold current ASOCSA accreditation.
                All formwork, reinforcement cover, and curing to comply with SANS 10100-1 clauses 6, 7, and 9.
                """,
            reference: "SANS 10100-1",
            keyword: "concrete"
        ),
        new(
            ["dpc", "damp-proof", "dampproof", "damp proof"],
            section: "Substructure",
            heading: "Damp-Proof Course (DPC)",
            text: """
                A continuous horizontal damp-proof course to be installed at floor slab level and at every
                change in level in masonry walls. DPC to comply with SANS 952 (Damp-proof courses and
                membranes). Material: minimum 0.25mm thick polyethylene or approved bituminous felt.
                All laps minimum 150mm. DPC to be turned up behind skirting and dressed to fall to outside.
                """,
            reference: "SANS 952",
            keyword: "dpc"
        ),
        new(
            ["screed", "floor screed"],
            section: "Substructure",
            heading: "Floor Screed",
            text: """
                Floor screed to be minimum 50mm cement:sand (1:3) screed unless otherwise noted.
                Screed to be power-floated to a tolerance of ±3mm under a 3m straightedge.
                Control joints to be provided at maximum 6m centres and at all structural joints.
                Screed to be kept moist-cured for minimum 7 days before tiling or floor covering.
                """,
            reference: "SANS 10400-F",
            keyword: "screed"
        ),
        new(
            ["bitumen", "bituminous", "torch-on", "torchon", "waterproof"],
            section: "Substructure",
            heading: "Bituminous Waterproofing",
            text: """
                All below-slab and wet-area waterproofing to comply with SANS 10021 (Waterproofing of
                buildings). Torch-on waterproofing membranes to be minimum 4mm APP or SBS modified
                bitumen with reinforcing fleece. Applied by an approved specialist waterproofing
                contractor. All laps minimum 100mm and fully bonded. Test by flooding for 24 hours
                before concrete topping is placed.
                """,
            reference: "SANS 10021",
            keyword: "bitumen"
        ),

        // ── Superstructure – Walling ─────────────────────────────────────────
        new(
            ["brick", "face brick", "face-brick", "facebrick", "clinker", "clay brick"],
            section: "Superstructure",
            heading: "Face Brick Masonry",
            text: """
                Face brick to comply with SANS 227 (Burnt clay masonry units). Minimum compressive
                strength: Class MU (≥ 14 MPa) for above-DPC walling; Class MU (≥ 20 MPa) for below-
                DPC walling. All face brickwork to be laid with full mortar joints in a stretcher bond
                pattern unless otherwise shown. Mortar mix: 1:1:6 (cement:lime:sand) to SANS 2001-CM1.
                All perpends to be plumb and bed joints level. External face brick faces to remain
                uncoated. Clean down on completion with approved proprietary brick cleaner.
                """,
            reference: "SANS 227; SANS 2001-CM1",
            keyword: "brick"
        ),
        new(
            ["plaster", "render", "plasterwork", "roughcast"],
            section: "Superstructure",
            heading: "Cement Plaster",
            text: """
                External plaster: 15mm two-coat cement plaster to SANS 2001-CM3, mix 1:4 (cement:sand).
                Undercoat (10mm) to be scratch-keyed before applying finishing coat (5mm).
                Internal plaster: 10–13mm single coat to mix 1:5 (cement:sand).
                All plasterwork to be kept moist-cured for minimum 3 days. Plaster to be applied
                only when ambient temperature is between 5°C and 35°C.
                """,
            reference: "SANS 2001-CM3",
            keyword: "plaster"
        ),
        new(
            ["blockwork", "block", "masonry block", "concrete block", "hollow block"],
            section: "Superstructure",
            heading: "Concrete Masonry Blockwork",
            text: """
                Concrete masonry units to comply with SANS 1215 (Concrete masonry units).
                Minimum compressive strength: 7 MPa for non-loadbearing; 14 MPa for loadbearing.
                Mortar: mix 1:1:5 (cement:lime:sand) to SANS 2001-CM1.
                Control joints at maximum 6m centres and at all openings and returns.
                Block bonding: minimum 25% overlap. Reinforced masonry lintels to SANS 10164-1.
                """,
            reference: "SANS 1215; SANS 10164-1",
            keyword: "blockwork"
        ),
        new(
            ["dryvit", "etics", "insulated panel", "polystyrene", "eps panel", "nutec", "fibre cement",
             "gypsum board", "gyproc", "drylining", "dry lining", "plasterboard"],
            section: "Superstructure",
            heading: "Lightweight / Dry-Walling Systems",
            text: """
                Lightweight walling systems (fibre cement, gypsum board, or ETICS) to be installed
                strictly in accordance with the manufacturer's specifications and approved shop drawings.
                Fibre cement products to comply with SANS 803 (Fibre cement flat sheets).
                Gypsum plasterboard to comply with SANS 1322 (Gypsum plasterboard).
                All joints to be taped and filled with approved compound. Fixing centres as per
                manufacturer's specification. Fire rating to comply with SANS 10400-T requirements.
                """,
            reference: "SANS 803; SANS 1322; SANS 10400-T",
            keyword: "fibre cement"
        ),

        // ── Superstructure – Structure ───────────────────────────────────────
        new(
            ["steel", "structural steel", "steelwork", "stainless steel"],
            section: "Superstructure",
            heading: "Structural Steelwork",
            text: """
                Structural steelwork to comply with SANS 10162-1 (The structural use of steel – limit-
                states design of hot-rolled steelwork). All steelwork to be fabricated and erected in
                accordance with approved shop drawings. Steel sections to comply with SANS 657 or
                SANS 50052 (as applicable). All exposed steelwork to be prepared to Sa 2.5 (ISO 8501-1)
                and coated with one primer coat and two finishing coats of approved anti-corrosion paint.
                Hot-dip galvanising (minimum 85 μm) to SANS 121 where specified.
                """,
            reference: "SANS 10162-1; SANS 121",
            keyword: "steel"
        ),
        new(
            ["timber", "wood", "wooden", "softwood", "hardwood", "pine", "meranti", "sa pine",
             "laminated timber", "glulam", "engineered wood", "lvl"],
            section: "Superstructure",
            heading: "Structural Timber",
            text: """
                All structural timber to comply with SANS 10163 (The structural use of timber).
                Timber to be stress-graded and visually graded to SANS 1783-1 or SANS 1783-2.
                All structural timber to be preservative-treated to SABS 10005 (Preservation of timber):
                H3 treatment for exposed above-ground use; H4 for in-ground contact.
                Moisture content at time of installation: ≤ 15% (SANS 10163).
                Connector plates, bolts, and other fasteners to be hot-dip galvanised or stainless steel.
                """,
            reference: "SANS 10163; SABS 10005",
            keyword: "timber"
        ),

        // ── Roofing ─────────────────────────────────────────────────────────
        new(
            ["ibr", "roof sheet", "corrugated", "corrugated iron", "metal roof", "roof sheeting",
             "galvanised", "zincalume", "colorbond", "klip-lok", "kliplok", "standing seam"],
            section: "Roofing",
            heading: "Metal Roof Sheeting",
            text: """
                IBR or corrugated roof sheeting to comply with SANS 6225 (Profiled steel sheets).
                Minimum thickness: 0.47mm (BMT) Zincalume coated to SANS 4998 unless otherwise specified.
                Minimum pitch as per sheet manufacturer's specification and SANS 10400-L (Roofs).
                Side laps minimum 1½ corrugations; end laps minimum 200mm.
                All fasteners to be self-drilling hex-head screws with neoprene washer seals.
                Gutters and downpipes to comply with SANS 10400-R.
                """,
            reference: "SANS 6225; SANS 10400-L",
            keyword: "ibr"
        ),
        new(
            ["roof tile", "clay tile", "concrete tile", "terracotta tile", "rooftile", "interlocking tile"],
            section: "Roofing",
            heading: "Roof Tiling",
            text: """
                Roof tiles to comply with SANS 542 (Burnt clay roofing tiles) or SANS 753 (Concrete
                roofing tiles) as applicable. Tiles to be fixed on pressure-treated timber battens
                at centres as per tile manufacturer's specification. Minimum lap and pitch as per
                SANS 10400-L (Roofs). All ridges, hips, and valleys to be bedded and pointed in
                cement mortar 1:3 or fixed with purpose-made dry-fix system.
                """,
            reference: "SANS 542; SANS 753; SANS 10400-L",
            keyword: "roof tile"
        ),
        new(
            ["insulation", "thermal insulation", "roof insulation", "glasswool", "rockwool",
             "polyisocyanurate", "pir", "xps", "rigid board"],
            section: "Roofing",
            heading: "Roof and Wall Insulation",
            text: """
                Thermal insulation to achieve minimum R-values required by SANS 10400-XA (Energy usage
                in buildings) for the applicable climate zone. Insulation products to carry a valid
                SABS mark or equivalent third-party product certification.
                Roof insulation: minimum R-value of 3.7 m²·K/W (Climate Zone 1) to 5.0 m²·K/W
                (Climate Zone 6). Install insulation to manufacturer's recommendations.
                All insulation products to comply with SANS 10177-5 (fire testing).
                """,
            reference: "SANS 10400-XA; SANS 10177-5",
            keyword: "insulation"
        ),

        // ── Openings (Doors & Windows) ───────────────────────────────────────
        new(
            ["aluminium window", "aluminum window", "aluminium frame", "alu window", "alum window",
             "aluminium", "aluminum"],
            section: "Openings",
            heading: "Aluminium Windows and Doors",
            text: """
                Aluminium window and door frames to comply with SANS 613 (Aluminium alloy windows)
                and SANS 1243 (Aluminium doors). All aluminium sections to be minimum 6063-T5 alloy.
                Frames to be thermally broken where required by SANS 10400-XA for energy efficiency.
                Powder coating to comply with SANS 1578 (Powder organic coatings), minimum 60 μm DFT.
                All gaskets, seals, and hardware to be compatible with aluminium and UV-stabilised.
                Weather stripping on all openable lights to provide airtight seal when closed.
                """,
            reference: "SANS 613; SANS 1243; SANS 10400-XA",
            keyword: "aluminium"
        ),
        new(
            ["timber door", "wooden door", "solid door", "flush door", "panel door", "door",
             "door frame"],
            section: "Openings",
            heading: "Timber Doors",
            text: """
                External timber doors to comply with SANS 1767 (Timber doors). External doors to be
                solid-core construction with hardwood lipping, minimum 44mm thick.
                Internal doors: minimum 35mm solid or hollow-core flush doors.
                All timber doors to be primed on all six faces before installation.
                External door frames to be minimum 102 × 69mm timber, preservative-treated to H3.
                Door ironmongery to comply with SANS 10400-D. Locks and handles to be of approved quality.
                """,
            reference: "SANS 1767; SANS 10400-D",
            keyword: "timber door"
        ),
        new(
            ["glass", "glazing", "double glaze", "double-glaze", "low-e", "lowe", "argon",
             "safety glass", "laminated glass", "toughened glass", "tempered"],
            section: "Openings",
            heading: "Glazing",
            text: """
                All glazing to comply with SANS 10400-N (Glazing). Safety glazing (toughened or
                laminated) to comply with SANS 1263-1 in all critical locations as defined by
                SANS 10400-N, including glazing within 300mm of floor level, in doors, and in
                side panels adjacent to doors. Double-glazed units to comply with SANS 1431.
                Solar control glazing (Low-E or tinted) to achieve SHGC ≤ 0.40 in climate zones
                with high solar exposure (SANS 10400-XA).
                Minimum glass thickness: 4mm for panes up to 1.5m²; 6mm for larger panes.
                """,
            reference: "SANS 10400-N; SANS 1263-1; SANS 1431",
            keyword: "glass"
        ),

        // ── Finishes ────────────────────────────────────────────────────────
        new(
            ["ceramic tile", "floor tile", "wall tile", "tile", "tiling", "porcelain", "slate"],
            section: "Finishes",
            heading: "Ceramic / Porcelain Tiling",
            text: """
                Floor and wall tiles to comply with SANS 1381 (Ceramic tiles – specifications).
                Floor tiles: minimum PEI 4 rating for domestic floors; PEI 5 for commercial.
                Tiles to be fixed with approved flexible tile adhesive to SANS 1960 on a sound,
                cured substrate. Grout joints: minimum 1.5mm, filled with matching coloured grout.
                All wet area and external tiling to be on a fully waterproofed substrate.
                Expansion joints at maximum 4.5m centres and at all changes in backing material.
                """,
            reference: "SANS 1381; SANS 10107",
            keyword: "tile"
        ),
        new(
            ["paint", "painting", "emulsion", "enamel", "coat", "primer", "varnish", "sealer"],
            section: "Finishes",
            heading: "Paint Finishes",
            text: """
                All paint systems to comply with SANS 10400-TT (Painting) and the South African
                Bureau of Standards approved paint specification.
                External masonry: alkali-resistant primer + 2 coats elastomeric exterior emulsion.
                Internal walls: 1 coat PVA sealer + 2 coats interior emulsion, eggshell finish.
                Metalwork: 1 coat zinc phosphate primer + 2 coats gloss enamel.
                All paints to carry a valid SABS mark. Surfaces to be prepared (clean, dry, sound)
                before application. Minimum curing time between coats as per manufacturer.
                """,
            reference: "SANS 10400-TT",
            keyword: "paint"
        ),

        // ── Services / MEP ───────────────────────────────────────────────────
        new(
            ["pvc pipe", "upvc", "cpvc", "hdpe pipe", "polypipe", "drainage pipe",
             "sewer", "soil pipe", "waste pipe"],
            section: "Services",
            heading: "Drainage and Plumbing Pipework",
            text: """
                Underground drainage pipework to comply with SANS 791 (uPVC sewer pipes).
                Above-ground soil and waste pipes to comply with SANS 967 (Plastics pipe systems –
                ABS/uPVC). All pipework to be installed to SANS 10400-P (Drainage) and SANS 10252
                (Water supply and drainage for buildings).
                Minimum gradients: 1:40 for 50mm; 1:60 for 75mm; 1:80 for 100mm diameter.
                All connections to existing municipal services require approved plumber's sign-off.
                """,
            reference: "SANS 791; SANS 10252; SANS 10400-P",
            keyword: "pvc pipe"
        ),
        new(
            ["copper pipe", "copper tube", "press fitting", "soldered"],
            section: "Services",
            heading: "Copper Water Supply Pipework",
            text: """
                Copper pipework for potable water to comply with SANS 460 (Copper tubes).
                All fittings to be compatible press-fit or solder-type to SANS 551.
                Hot water reticulation to be lagged with minimum 25mm armaflex or equivalent.
                Cold water supply: minimum 15mm diameter internal, 20mm external feed.
                Pressure testing: 1.5× operating pressure for minimum 30 minutes.
                Thermostatic mixing valves on all hot water outlets where required by SANS 10400-W.
                """,
            reference: "SANS 460; SANS 10400-W",
            keyword: "copper pipe"
        ),

        // ── General ─────────────────────────────────────────────────────────
        new(
            ["aggregate", "stone", "gravel", "crushed stone"],
            section: "Substructure",
            heading: "Aggregates",
            text: """
                All coarse and fine aggregates to comply with SANS 1083 (Aggregates from natural
                sources for concrete). Aggregate to be clean, hard, and free from deleterious
                material. Maximum aggregate size: 19mm for slabs; 26mm for foundations.
                Concrete mix designs to state aggregate source and grading. No sea sand to be used
                without approval and specialist treatment.
                """,
            reference: "SANS 1083",
            keyword: "aggregate"
        ),
        new(
            ["prefab", "precast", "precast concrete", "pre-cast", "precast slab"],
            section: "Superstructure",
            heading: "Precast Concrete Elements",
            text: """
                Precast concrete elements to be designed and manufactured to SANS 10100-2 and
                SANS 1200G (Concrete works). Precaster to hold current ASOCSA product certification.
                All elements to be proof-loaded and tested at works before delivery.
                Bearing and fixing details per structural engineer's specification.
                All joints between precast elements to be packed and pointed with non-shrink grout.
                """,
            reference: "SANS 10100-2; SANS 1200G",
            keyword: "precast"
        ),
    ];

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Compiles a structured specification from a list of raw IFC material strings.
    /// </summary>
    public CompiledSpec Compile(SpecCompileRequest request)
    {
        var detected = new List<DetectedMaterial>();
        var clausesByKey = new Dictionary<string, SpecClause>(StringComparer.OrdinalIgnoreCase);
        var unmatched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Tokenise all material strings into normalised keywords
        var allTokens = request.MaterialStrings
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .SelectMany(s => Tokenise(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var token in allTokens)
        {
            bool matched = false;
            foreach (var rule in Rules)
            {
                if (rule.Matches(token))
                {
                    matched = true;
                    if (!clausesByKey.ContainsKey(rule.Keyword))
                    {
                        clausesByKey[rule.Keyword] = new SpecClause
                        {
                            Heading = rule.Heading,
                            Text = CleanText(rule.Text),
                            Reference = rule.Reference,
                            Section = rule.Section,
                            TriggerKeyword = rule.Keyword,
                        };
                        detected.Add(new DetectedMaterial
                        {
                            RawValue = token,
                            Keyword = rule.Keyword,
                            ElementCategory = "",
                        });
                    }
                    break;
                }
            }
            if (!matched && token.Length > 2)
            {
                unmatched.Add(token);
            }
        }

        // Group clauses into ordered sections
        var sectionOrder = new[]
        {
            "Substructure", "Superstructure", "Roofing", "Openings", "Finishes", "Services",
        };

        var sections = sectionOrder
            .Select(sName => new SpecSection
            {
                Name = sName,
                Clauses = clausesByKey.Values
                    .Where(c => c.Section == sName)
                    .OrderBy(c => c.Heading)
                    .ToList(),
            })
            .Where(s => s.Clauses.Count > 0)
            .ToList();

        return new CompiledSpec
        {
            Sections = sections,
            DetectedMaterials = detected,
            UnmatchedKeywords = [.. unmatched.OrderBy(k => k)],
            ClauseCount = clausesByKey.Count,
            GeneratedAt = DateTime.UtcNow,
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Splits a raw IFC string (e.g. "Brick-110mm-Cavity-PlasterFinish") into
    /// lower-case tokens by splitting on punctuation, whitespace, and digit runs.
    /// </summary>
    private static IEnumerable<string> Tokenise(string raw)
    {
        // Split on delimiters and digit sequences, keeping multi-word substrings
        var parts = Regex.Split(raw.ToLowerInvariant(), @"[\-_\.\:\|\\\/\(\)\[\],;]+")
            .Select(p => p.Trim())
            .Where(p => p.Length > 1)
            .ToList();

        // Also yield the original normalised string so multi-word patterns ("face brick") can match
        yield return raw.ToLowerInvariant().Replace('-', ' ').Trim();

        foreach (var p in parts)
        {
            if (!string.IsNullOrWhiteSpace(p)) yield return p;
        }
    }

    /// <summary>Removes leading whitespace/indentation from multi-line literal strings.</summary>
    private static string CleanText(string text)
    {
        var lines = text.Split('\n');
        var cleaned = lines
            .Select(l => l.TrimStart())
            .Where(l => l.Length > 0);
        return string.Join(" ", cleaned).Trim();
    }

    // ── Rule definition ───────────────────────────────────────────────────────

    private sealed class SpecRule(
        IEnumerable<string> patterns,
        string section,
        string heading,
        string text,
        string reference,
        string keyword)
    {
        private readonly string[] _patterns = [.. patterns];

        public string Section { get; } = section;
        public string Heading { get; } = heading;
        public string Text { get; } = text;
        public string Reference { get; } = reference;
        public string Keyword { get; } = keyword;

        public bool Matches(string token) =>
            _patterns.Any(p => token.Contains(p, StringComparison.OrdinalIgnoreCase));
    }
}
