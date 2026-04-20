-- ============================================================
-- Seed: 02_municipalities
-- All South African municipalities (Metropolitan, District, Local).
-- Categories: A = Metropolitan, B = Local, C = District
-- Source: municipalities.co.za (April 2026)
-- ============================================================

with provinces as (
    select id, code from public.provinces
)
insert into public.municipalities (province_id, name, short_name, category, zoning_scheme)
select p.id, m.name, m.short_name, m.category, m.zoning_scheme
from provinces p
join (values

    -- ══════════════════════════════════════════════════════════
    -- METROPOLITAN MUNICIPALITIES (Category A)
    -- ══════════════════════════════════════════════════════════

    -- ── Eastern Cape ─────────────────────────────────────────
    ('EC',  'Buffalo City Metropolitan Municipality',                  'Buffalo City',          'A', 'Buffalo City Metro Municipality Zoning Scheme'),
    ('EC',  'Nelson Mandela Bay Metropolitan Municipality',            'NMB Metro',             'A', 'Nelson Mandela Bay Spatial Planning and Land Use Management By-Law'),

    -- ── Free State ───────────────────────────────────────────
    ('FS',  'Mangaung Metropolitan Municipality',                      'Mangaung',              'A', 'Mangaung Metropolitan Municipality Planning By-Law'),

    -- ── Gauteng ──────────────────────────────────────────────
    ('GP',  'City of Ekurhuleni Metropolitan Municipality',            'Ekurhuleni',            'A', 'Ekurhuleni Metropolitan Municipality Town Planning Scheme 2014'),
    ('GP',  'City of Johannesburg Metropolitan Municipality',          'Johannesburg',          'A', 'City of Johannesburg Municipal Planning By-Law 2016'),
    ('GP',  'City of Tshwane Metropolitan Municipality',               'Tshwane',               'A', 'Tshwane Town Planning Scheme 2008 (as revised)'),

    -- ── KwaZulu-Natal ────────────────────────────────────────
    ('KZN', 'eThekwini Metropolitan Municipality',                     'eThekwini',             'A', 'eThekwini Municipality Scheme'),

    -- ── Western Cape ─────────────────────────────────────────
    ('WC',  'City of Cape Town Metropolitan Municipality',             'Cape Town',             'A', 'City of Cape Town Municipal Planning By-Law 2015'),

    -- ══════════════════════════════════════════════════════════
    -- DISTRICT MUNICIPALITIES (Category C)
    -- ══════════════════════════════════════════════════════════

    -- ── Eastern Cape ─────────────────────────────────────────
    ('EC',  'Alfred Nzo District Municipality',                        'Alfred Nzo',            'C', 'Alfred Nzo District Municipality Spatial Planning By-Law'),
    ('EC',  'Amathole District Municipality',                          'Amathole',              'C', 'Amathole District Municipality Spatial Planning By-Law'),
    ('EC',  'Chris Hani District Municipality',                        'Chris Hani',            'C', 'Chris Hani District Municipality Spatial Planning By-Law'),
    ('EC',  'Joe Gqabi District Municipality',                         'Joe Gqabi',             'C', 'Joe Gqabi District Municipality Spatial Planning By-Law'),
    ('EC',  'OR Tambo District Municipality',                          'OR Tambo',              'C', 'OR Tambo District Municipality Spatial Planning By-Law'),
    ('EC',  'Sarah Baartman District Municipality',                    'Sarah Baartman',        'C', 'Sarah Baartman District Municipality Spatial Planning By-Law'),

    -- ── Free State ───────────────────────────────────────────
    ('FS',  'Fezile Dabi District Municipality',                       'Fezile Dabi',           'C', 'Fezile Dabi District Municipality Spatial Planning By-Law'),
    ('FS',  'Lejweleputswa District Municipality',                     'Lejweleputswa',         'C', 'Lejweleputswa District Municipality Spatial Planning By-Law'),
    ('FS',  'Thabo Mofutsanyana District Municipality',                'Thabo Mofutsanyana',    'C', 'Thabo Mofutsanyana District Municipality Spatial Planning By-Law'),
    ('FS',  'Xhariep District Municipality',                           'Xhariep',               'C', 'Xhariep District Municipality Spatial Planning By-Law'),

    -- ── Gauteng ──────────────────────────────────────────────
    ('GP',  'Sedibeng District Municipality',                          'Sedibeng',              'C', 'Sedibeng District Municipality Spatial Planning By-Law'),
    ('GP',  'West Rand District Municipality',                         'West Rand',             'C', 'West Rand District Municipality Spatial Planning By-Law'),

    -- ── KwaZulu-Natal ────────────────────────────────────────
    ('KZN', 'Amajuba District Municipality',                           'Amajuba',               'C', 'Amajuba District Municipality Spatial Planning By-Law'),
    ('KZN', 'Harry Gwala District Municipality',                       'Harry Gwala',           'C', 'Harry Gwala District Municipality Spatial Planning By-Law'),
    ('KZN', 'iLembe District Municipality',                            'iLembe',                'C', 'iLembe District Municipality Spatial Planning By-Law'),
    ('KZN', 'King Cetshwayo District Municipality',                    'King Cetshwayo',        'C', 'King Cetshwayo District Municipality Spatial Planning By-Law'),
    ('KZN', 'uGu District Municipality',                               'uGu',                   'C', 'uGu District Municipality Spatial Planning By-Law'),
    ('KZN', 'uMgungundlovu District Municipality',                     'uMgungundlovu',         'C', 'uMgungundlovu District Municipality Spatial Planning By-Law'),
    ('KZN', 'uMkhanyakude District Municipality',                      'uMkhanyakude',          'C', 'uMkhanyakude District Municipality Spatial Planning By-Law'),
    ('KZN', 'uMzinyathi District Municipality',                        'uMzinyathi',            'C', 'uMzinyathi District Municipality Spatial Planning By-Law'),
    ('KZN', 'uThukela District Municipality',                          'uThukela',              'C', 'uThukela District Municipality Spatial Planning By-Law'),
    ('KZN', 'Zululand District Municipality',                          'Zululand',              'C', 'Zululand District Municipality Spatial Planning By-Law'),

    -- ── Limpopo ──────────────────────────────────────────────
    ('LP',  'Capricorn District Municipality',                         'Capricorn',             'C', 'Capricorn District Municipality Spatial Planning By-Law'),
    ('LP',  'Mopani District Municipality',                            'Mopani',                'C', 'Mopani District Municipality Spatial Planning By-Law'),
    ('LP',  'Sekhukhune District Municipality',                        'Sekhukhune',            'C', 'Sekhukhune District Municipality Spatial Planning By-Law'),
    ('LP',  'Vhembe District Municipality',                            'Vhembe',                'C', 'Vhembe District Municipality Spatial Planning By-Law'),
    ('LP',  'Waterberg District Municipality',                         'Waterberg',             'C', 'Waterberg District Municipality Spatial Planning By-Law'),

    -- ── Mpumalanga ───────────────────────────────────────────
    ('MP',  'Ehlanzeni District Municipality',                         'Ehlanzeni',             'C', 'Ehlanzeni District Municipality Spatial Planning By-Law'),
    ('MP',  'Gert Sibande District Municipality',                      'Gert Sibande',          'C', 'Gert Sibande District Municipality Spatial Planning By-Law'),
    ('MP',  'Nkangala District Municipality',                          'Nkangala',              'C', 'Nkangala District Municipality Spatial Planning By-Law'),

    -- ── Northern Cape ────────────────────────────────────────
    ('NC',  'Frances Baard District Municipality',                     'Frances Baard',         'C', 'Frances Baard District Municipality Spatial Planning By-Law'),
    ('NC',  'John Taolo Gaetsewe District Municipality',               'John Taolo Gaetsewe',   'C', 'John Taolo Gaetsewe District Municipality Spatial Planning By-Law'),
    ('NC',  'Namakwa District Municipality',                           'Namakwa',               'C', 'Namakwa District Municipality Spatial Planning By-Law'),
    ('NC',  'Pixley Ka Seme District Municipality',                    'Pixley Ka Seme',        'C', 'Pixley Ka Seme District Municipality Spatial Planning By-Law'),
    ('NC',  'ZF Mgcawu District Municipality',                         'ZF Mgcawu',             'C', 'ZF Mgcawu District Municipality Spatial Planning By-Law'),

    -- ── North West ───────────────────────────────────────────
    ('NW',  'Bojanala Platinum District Municipality',                  'Bojanala Platinum',     'C', 'Bojanala Platinum District Municipality Spatial Planning By-Law'),
    ('NW',  'Dr Kenneth Kaunda District Municipality',                  'Dr Kenneth Kaunda',     'C', 'Dr Kenneth Kaunda District Municipality Spatial Planning By-Law'),
    ('NW',  'Dr Ruth Segomotsi Mompati District Municipality',          'Dr Ruth S Mompati',     'C', 'Dr Ruth Segomotsi Mompati District Municipality Spatial Planning By-Law'),
    ('NW',  'Ngaka Modiri Molema District Municipality',                'Ngaka Modiri Molema',   'C', 'Ngaka Modiri Molema District Municipality Spatial Planning By-Law'),

    -- ── Western Cape ─────────────────────────────────────────
    ('WC',  'Cape Winelands District Municipality',                    'Cape Winelands',        'C', 'Cape Winelands District Municipality Spatial Planning By-Law'),
    ('WC',  'Central Karoo District Municipality',                     'Central Karoo',         'C', 'Central Karoo District Municipality Spatial Planning By-Law'),
    ('WC',  'Garden Route District Municipality',                      'Garden Route',          'C', 'Garden Route District Municipality Spatial Planning By-Law'),
    ('WC',  'Overberg District Municipality',                          'Overberg',              'C', 'Overberg District Municipality Spatial Planning By-Law'),
    ('WC',  'West Coast District Municipality',                        'West Coast',            'C', 'West Coast District Municipality Spatial Planning By-Law'),

    -- ══════════════════════════════════════════════════════════
    -- LOCAL MUNICIPALITIES (Category B)
    -- ══════════════════════════════════════════════════════════

    -- ── Eastern Cape ─────────────────────────────────────────
    ('EC',  'Amahlathi Local Municipality',                            'Amahlathi',             'B', 'Amahlathi Land Use Management Scheme'),
    ('EC',  'Blue Crane Route Local Municipality',                     'Blue Crane Route',      'B', 'Blue Crane Route Spatial Planning By-Law'),
    ('EC',  'Dr AB Xuma Local Municipality',                           'Dr AB Xuma',            'B', 'Dr AB Xuma Spatial Planning By-Law'),
    ('EC',  'Dr Beyers Naudé Local Municipality',                      'Dr Beyers Naudé',       'B', 'Dr Beyers Naudé Spatial Planning By-Law'),
    ('EC',  'Elundini Local Municipality',                             'Elundini',              'B', 'Elundini Spatial Planning By-Law'),
    ('EC',  'Emalahleni Local Municipality',                           'Emalahleni',            'B', 'Emalahleni Spatial Planning By-Law'),
    ('EC',  'Enoch Mgijima Local Municipality',                        'Enoch Mgijima',         'B', 'Enoch Mgijima Spatial Planning By-Law'),
    ('EC',  'Great Kei Local Municipality',                            'Great Kei',             'B', 'Great Kei Spatial Planning By-Law'),
    ('EC',  'Ingquza Hill Local Municipality',                         'Ingquza Hill',          'B', 'Ingquza Hill Spatial Planning By-Law'),
    ('EC',  'Intsika Yethu Local Municipality',                        'Intsika Yethu',         'B', 'Intsika Yethu Spatial Planning By-Law'),
    ('EC',  'Inxuba Yethemba Local Municipality',                      'Inxuba Yethemba',       'B', 'Inxuba Yethemba Spatial Planning By-Law'),
    ('EC',  'King Sabata Dalindyebo Local Municipality',               'King Sabata Dalindyebo','B', 'King Sabata Dalindyebo Spatial Planning By-Law'),
    ('EC',  'Kouga Local Municipality',                                'Kouga',                 'B', 'Kouga Land Use Management Scheme'),
    ('EC',  'Koukamma Local Municipality',                             'Koukamma',              'B', 'Koukamma Spatial Planning By-Law'),
    ('EC',  'Makana Local Municipality',                               'Makana',                'B', 'Makana Land Use Management Scheme'),
    ('EC',  'Matatiele Local Municipality',                            'Matatiele',             'B', 'Matatiele Spatial Planning By-Law'),
    ('EC',  'Mbhashe Local Municipality',                              'Mbhashe',               'B', 'Mbhashe Spatial Planning By-Law'),
    ('EC',  'Mhlontlo Local Municipality',                             'Mhlontlo',              'B', 'Mhlontlo Spatial Planning By-Law'),
    ('EC',  'Mnquma Local Municipality',                               'Mnquma',                'B', 'Mnquma Spatial Planning By-Law'),
    ('EC',  'Ndlambe Local Municipality',                              'Ndlambe',               'B', 'Ndlambe Spatial Planning By-Law'),
    ('EC',  'Ngqushwa Local Municipality',                             'Ngqushwa',              'B', 'Ngqushwa Spatial Planning By-Law'),
    ('EC',  'Ntabankulu Local Municipality',                           'Ntabankulu',            'B', 'Ntabankulu Spatial Planning By-Law'),
    ('EC',  'Nyandeni Local Municipality',                             'Nyandeni',              'B', 'Nyandeni Spatial Planning By-Law'),
    ('EC',  'Port St Johns Local Municipality',                        'Port St Johns',         'B', 'Port St Johns Spatial Planning By-Law'),
    ('EC',  'Raymond Mhlaba Local Municipality',                       'Raymond Mhlaba',        'B', 'Raymond Mhlaba Spatial Planning By-Law'),
    ('EC',  'Sakhisizwe Local Municipality',                           'Sakhisizwe',            'B', 'Sakhisizwe Spatial Planning By-Law'),
    ('EC',  'Senqu Local Municipality',                                'Senqu',                 'B', 'Senqu Spatial Planning By-Law'),
    ('EC',  'Sundays River Valley Local Municipality',                 'Sundays River Valley',  'B', 'Sundays River Valley Spatial Planning By-Law'),
    ('EC',  'Umzimvubu Local Municipality',                            'Umzimvubu',             'B', 'Umzimvubu Spatial Planning By-Law'),
    ('EC',  'Walter Sisulu Local Municipality',                        'Walter Sisulu',         'B', 'Walter Sisulu Spatial Planning By-Law'),
    ('EC',  'Winnie Madikizela-Mandela Local Municipality',            'Winnie M-Mandela',      'B', 'Winnie Madikizela-Mandela Spatial Planning By-Law'),

    -- ── Free State ───────────────────────────────────────────
    ('FS',  'Dihlabeng Local Municipality',                            'Dihlabeng',             'B', 'Dihlabeng Spatial Planning By-Law'),
    ('FS',  'Kopanong Local Municipality',                             'Kopanong',              'B', 'Kopanong Spatial Planning By-Law'),
    ('FS',  'Letsemeng Local Municipality',                            'Letsemeng',             'B', 'Letsemeng Spatial Planning By-Law'),
    ('FS',  'Mafube Local Municipality',                               'Mafube',                'B', 'Mafube Spatial Planning By-Law'),
    ('FS',  'Maluti-A-Phofung Local Municipality',                     'Maluti-A-Phofung',      'B', 'Maluti-A-Phofung Spatial Planning By-Law'),
    ('FS',  'Mantsopa Local Municipality',                             'Mantsopa',              'B', 'Mantsopa Spatial Planning By-Law'),
    ('FS',  'Masilonyana Local Municipality',                          'Masilonyana',           'B', 'Masilonyana Spatial Planning By-Law'),
    ('FS',  'Matjhabeng Local Municipality',                           'Matjhabeng',            'B', 'Matjhabeng Town Planning Scheme'),
    ('FS',  'Metsimaholo Local Municipality',                          'Metsimaholo',           'B', 'Metsimaholo Spatial Planning By-Law'),
    ('FS',  'Mohokare Local Municipality',                             'Mohokare',              'B', 'Mohokare Spatial Planning By-Law'),
    ('FS',  'Moqhaka Local Municipality',                              'Moqhaka',               'B', 'Moqhaka Spatial Planning By-Law'),
    ('FS',  'Nala Local Municipality',                                 'Nala',                  'B', 'Nala Spatial Planning By-Law'),
    ('FS',  'Ngwathe Local Municipality',                              'Ngwathe',               'B', 'Ngwathe Spatial Planning By-Law'),
    ('FS',  'Nketoana Local Municipality',                             'Nketoana',              'B', 'Nketoana Spatial Planning By-Law'),
    ('FS',  'Phumelela Local Municipality',                            'Phumelela',             'B', 'Phumelela Spatial Planning By-Law'),
    ('FS',  'Setsoto Local Municipality',                              'Setsoto',               'B', 'Setsoto Spatial Planning By-Law'),
    ('FS',  'Tokologo Local Municipality',                             'Tokologo',              'B', 'Tokologo Spatial Planning By-Law'),
    ('FS',  'Tswelopele Local Municipality',                           'Tswelopele',            'B', 'Tswelopele Spatial Planning By-Law'),

    -- ── Gauteng ──────────────────────────────────────────────
    ('GP',  'Emfuleni Local Municipality',                             'Emfuleni',              'B', 'Emfuleni Municipality Spatial Planning By-Law'),
    ('GP',  'Lesedi Local Municipality',                               'Lesedi',                'B', 'Lesedi Land Use Management Scheme'),
    ('GP',  'Merafong City Local Municipality',                        'Merafong City',         'B', 'Merafong City Spatial Planning By-Law'),
    ('GP',  'Midvaal Local Municipality',                              'Midvaal',               'B', 'Midvaal Local Municipality Land Use Scheme 2018'),
    ('GP',  'Mogale City Local Municipality',                          'Mogale City',           'B', 'Krugersdorp Town Planning Scheme 1980'),
    ('GP',  'Rand West City Local Municipality',                       'Rand West City',        'B', 'Randfontein Town Planning Scheme 1988'),

    -- ── KwaZulu-Natal ────────────────────────────────────────
    ('KZN', 'AbaQulusi Local Municipality',                            'AbaQulusi',             'B', 'AbaQulusi Spatial Planning By-Law'),
    ('KZN', 'Alfred Duma Local Municipality',                          'Alfred Duma',           'B', 'Alfred Duma Spatial Planning By-Law'),
    ('KZN', 'Big 5 Hlabisa Local Municipality',                        'Big 5 Hlabisa',         'B', 'Big 5 Hlabisa Spatial Planning By-Law'),
    ('KZN', 'City of uMhlathuze Local Municipality',                   'uMhlathuze',            'B', 'uMhlathuze Land Use Management Scheme'),
    ('KZN', 'Dannhauser Local Municipality',                           'Dannhauser',            'B', 'Dannhauser Spatial Planning By-Law'),
    ('KZN', 'Dr Nkosazana Dlamini Zuma Local Municipality',            'Dr Nkosazana DZ',       'B', 'Dr Nkosazana Dlamini Zuma Spatial Planning By-Law'),
    ('KZN', 'eDumbe Local Municipality',                               'eDumbe',                'B', 'eDumbe Spatial Planning By-Law'),
    ('KZN', 'eMadlangeni Local Municipality',                          'eMadlangeni',           'B', 'eMadlangeni Spatial Planning By-Law'),
    ('KZN', 'Endumeni Local Municipality',                             'Endumeni',              'B', 'Endumeni Spatial Planning By-Law'),
    ('KZN', 'Greater Kokstad Local Municipality',                      'Greater Kokstad',       'B', 'Greater Kokstad Spatial Planning By-Law'),
    ('KZN', 'Impendle Local Municipality',                             'Impendle',              'B', 'Impendle Spatial Planning By-Law'),
    ('KZN', 'Inkosi Langalibalele Local Municipality',                 'Inkosi Langalibalele',  'B', 'Inkosi Langalibalele Spatial Planning By-Law'),
    ('KZN', 'Inkosi uMtubatuba Local Municipality',                    'Inkosi uMtubatuba',     'B', 'Inkosi uMtubatuba Spatial Planning By-Law'),
    ('KZN', 'Johannes Phumani Phungula Local Municipality',            'Johannes PP',           'B', 'Johannes Phumani Phungula Spatial Planning By-Law'),
    ('KZN', 'Jozini Local Municipality',                               'Jozini',                'B', 'Jozini Spatial Planning By-Law'),
    ('KZN', 'KwaDukuza Local Municipality',                            'KwaDukuza',             'B', 'KwaDukuza Spatial Planning and Land Use Scheme'),
    ('KZN', 'Mandeni Local Municipality',                              'Mandeni',               'B', 'Mandeni Spatial Planning By-Law'),
    ('KZN', 'Maphumulo Local Municipality',                            'Maphumulo',             'B', 'Maphumulo Spatial Planning By-Law'),
    ('KZN', 'Mkhambathini Local Municipality',                         'Mkhambathini',          'B', 'Mkhambathini Spatial Planning By-Law'),
    ('KZN', 'Mpofana Local Municipality',                              'Mpofana',               'B', 'Mpofana Spatial Planning By-Law'),
    ('KZN', 'Msunduzi Local Municipality',                             'Msunduzi',              'B', 'Msunduzi Municipality Town Planning Scheme'),
    ('KZN', 'Mthonjaneni Local Municipality',                          'Mthonjaneni',           'B', 'Mthonjaneni Spatial Planning By-Law'),
    ('KZN', 'Ndwedwe Local Municipality',                              'Ndwedwe',               'B', 'Ndwedwe Spatial Planning By-Law'),
    ('KZN', 'Newcastle Local Municipality',                            'Newcastle',             'B', 'Newcastle Town Planning Scheme'),
    ('KZN', 'Nkandla Local Municipality',                              'Nkandla',               'B', 'Nkandla Spatial Planning By-Law'),
    ('KZN', 'Nongoma Local Municipality',                              'Nongoma',               'B', 'Nongoma Spatial Planning By-Law'),
    ('KZN', 'Nquthu Local Municipality',                               'Nquthu',                'B', 'Nquthu Spatial Planning By-Law'),
    ('KZN', 'Okhahlamba Local Municipality',                           'Okhahlamba',            'B', 'Okhahlamba Spatial Planning By-Law'),
    ('KZN', 'Ray Nkonyeni Local Municipality',                         'Ray Nkonyeni',          'B', 'Ray Nkonyeni Spatial Planning By-Law'),
    ('KZN', 'Richmond Local Municipality',                             'Richmond',              'B', 'Richmond Spatial Planning By-Law'),
    ('KZN', 'Ulundi Local Municipality',                               'Ulundi',                'B', 'Ulundi Spatial Planning By-Law'),
    ('KZN', 'Umdoni Local Municipality',                               'Umdoni',                'B', 'Umdoni Spatial Planning By-Law'),
    ('KZN', 'uMfolozi Local Municipality',                             'uMfolozi',              'B', 'uMfolozi Spatial Planning By-Law'),
    ('KZN', 'uMhlabuyalingana Local Municipality',                     'uMhlabuyalingana',      'B', 'uMhlabuyalingana Spatial Planning By-Law'),
    ('KZN', 'uMlalazi Local Municipality',                             'uMlalazi',              'B', 'uMlalazi Spatial Planning By-Law'),
    ('KZN', 'uMngeni Local Municipality',                              'uMngeni',               'B', 'uMngeni Spatial Planning By-Law'),
    ('KZN', 'uMshwathi Local Municipality',                            'uMshwathi',             'B', 'uMshwathi Spatial Planning By-Law'),
    ('KZN', 'uMsinga Local Municipality',                              'uMsinga',               'B', 'uMsinga Spatial Planning By-Law'),
    ('KZN', 'Umuziwabantu Local Municipality',                         'Umuziwabantu',          'B', 'Umuziwabantu Spatial Planning By-Law'),
    ('KZN', 'Umvoti Local Municipality',                               'Umvoti',                'B', 'Umvoti Spatial Planning By-Law'),
    ('KZN', 'Umzimkhulu Local Municipality',                           'Umzimkhulu',            'B', 'Umzimkhulu Spatial Planning By-Law'),
    ('KZN', 'Umzumbe Local Municipality',                              'Umzumbe',               'B', 'Umzumbe Spatial Planning By-Law'),
    ('KZN', 'uPhongolo Local Municipality',                            'uPhongolo',             'B', 'uPhongolo Spatial Planning By-Law'),

    -- ── Limpopo ──────────────────────────────────────────────
    ('LP',  'Ba-Phalaborwa Local Municipality',                        'Ba-Phalaborwa',         'B', 'Ba-Phalaborwa Spatial Planning By-Law'),
    ('LP',  'Bela-Bela Local Municipality',                            'Bela-Bela',             'B', 'Bela-Bela Spatial Planning By-Law'),
    ('LP',  'Blouberg Local Municipality',                             'Blouberg',              'B', 'Blouberg Spatial Planning By-Law'),
    ('LP',  'Collins Chabane Local Municipality',                      'Collins Chabane',       'B', 'Collins Chabane Spatial Planning By-Law'),
    ('LP',  'Elias Motsoaledi Local Municipality',                     'Elias Motsoaledi',      'B', 'Elias Motsoaledi Spatial Planning By-Law'),
    ('LP',  'Ephraim Mogale Local Municipality',                       'Ephraim Mogale',        'B', 'Ephraim Mogale Spatial Planning By-Law'),
    ('LP',  'Fetakgomo Tubatse Local Municipality',                    'Fetakgomo Tubatse',     'B', 'Fetakgomo Tubatse Spatial Planning By-Law'),
    ('LP',  'Greater Giyani Local Municipality',                       'Greater Giyani',        'B', 'Greater Giyani Spatial Planning By-Law'),
    ('LP',  'Greater Letaba Local Municipality',                       'Greater Letaba',        'B', 'Greater Letaba Spatial Planning By-Law'),
    ('LP',  'Greater Tzaneen Local Municipality',                      'Tzaneen',               'B', 'Greater Tzaneen Land Use Management Scheme'),
    ('LP',  'Lepelle-Nkumpi Local Municipality',                       'Lepelle-Nkumpi',        'B', 'Lepelle-Nkumpi Spatial Planning By-Law'),
    ('LP',  'Lephalale Local Municipality',                            'Lephalale',             'B', 'Lephalale Spatial Planning By-Law'),
    ('LP',  'Makhado Local Municipality',                              'Makhado',               'B', 'Makhado Spatial Planning By-Law'),
    ('LP',  'Makhuduthamaga Local Municipality',                       'Makhuduthamaga',        'B', 'Makhuduthamaga Spatial Planning By-Law'),
    ('LP',  'Maruleng Local Municipality',                             'Maruleng',              'B', 'Maruleng Spatial Planning By-Law'),
    ('LP',  'Modimolle-Mookgophong Local Municipality',                'Modimolle-Mookgophong', 'B', 'Modimolle-Mookgophong Spatial Planning By-Law'),
    ('LP',  'Mogalakwena Local Municipality',                          'Mogalakwena',           'B', 'Mogalakwena Spatial Planning By-Law'),
    ('LP',  'Molemole Local Municipality',                             'Molemole',              'B', 'Molemole Spatial Planning By-Law'),
    ('LP',  'Musina Local Municipality',                               'Musina',                'B', 'Musina Spatial Planning By-Law'),
    ('LP',  'Polokwane Local Municipality',                            'Polokwane',             'B', 'Polokwane Municipality Land Use Management By-Law'),
    ('LP',  'Thabazimbi Local Municipality',                           'Thabazimbi',            'B', 'Thabazimbi Spatial Planning By-Law'),
    ('LP',  'Thulamela Local Municipality',                            'Thulamela',             'B', 'Thulamela Spatial Planning By-Law'),

    -- ── Mpumalanga ───────────────────────────────────────────
    ('MP',  'Bushbuckridge Local Municipality',                        'Bushbuckridge',         'B', 'Bushbuckridge Spatial Planning By-Law'),
    ('MP',  'Chief Albert Luthuli Local Municipality',                 'Chief Albert Luthuli',  'B', 'Chief Albert Luthuli Spatial Planning By-Law'),
    ('MP',  'City of Mbombela Local Municipality',                     'Mbombela',              'B', 'Mbombela Land Use Management Scheme'),
    ('MP',  'Dipaleseng Local Municipality',                           'Dipaleseng',            'B', 'Dipaleseng Spatial Planning By-Law'),
    ('MP',  'Dr JS Moroka Local Municipality',                         'Dr JS Moroka',          'B', 'Dr JS Moroka Spatial Planning By-Law'),
    ('MP',  'Dr Pixley Ka Isaka Seme Local Municipality',              'Dr Pixley KIS',         'B', 'Dr Pixley Ka Isaka Seme Spatial Planning By-Law'),
    ('MP',  'eMalahleni Local Municipality',                           'eMalahleni',            'B', 'eMalahleni Spatial Planning By-Law'),
    ('MP',  'Emakhazeni Local Municipality',                           'Emakhazeni',            'B', 'Emakhazeni Spatial Planning By-Law'),
    ('MP',  'Govan Mbeki Local Municipality',                          'Govan Mbeki',           'B', 'Govan Mbeki Spatial Planning By-Law'),
    ('MP',  'Lekwa Local Municipality',                                'Lekwa',                 'B', 'Lekwa Spatial Planning By-Law'),
    ('MP',  'Mkhondo Local Municipality',                              'Mkhondo',               'B', 'Mkhondo Spatial Planning By-Law'),
    ('MP',  'Msukaligwa Local Municipality',                           'Msukaligwa',            'B', 'Msukaligwa Spatial Planning By-Law'),
    ('MP',  'Nkomazi Local Municipality',                              'Nkomazi',               'B', 'Nkomazi Spatial Planning By-Law'),
    ('MP',  'Steve Tshwete Local Municipality',                        'Steve Tshwete',         'B', 'Steve Tshwete Town Planning Scheme'),
    ('MP',  'Thaba Chweu Local Municipality',                          'Thaba Chweu',           'B', 'Thaba Chweu Spatial Planning By-Law'),
    ('MP',  'Thembisile Hani Local Municipality',                      'Thembisile Hani',       'B', 'Thembisile Hani Spatial Planning By-Law'),
    ('MP',  'Victor Khanye Local Municipality',                        'Victor Khanye',         'B', 'Victor Khanye Spatial Planning By-Law'),

    -- ── Northern Cape ────────────────────────────────────────
    ('NC',  '!Kheis Local Municipality',                               '!Kheis',                'B', '!Kheis Spatial Planning By-Law'),
    ('NC',  'Dawid Kruiper Local Municipality',                        'Dawid Kruiper',         'B', 'Dawid Kruiper Spatial Planning By-Law'),
    ('NC',  'Dikgatlong Local Municipality',                           'Dikgatlong',            'B', 'Dikgatlong Spatial Planning By-Law'),
    ('NC',  'Emthanjeni Local Municipality',                           'Emthanjeni',            'B', 'Emthanjeni Spatial Planning By-Law'),
    ('NC',  'Ga-Segonyana Local Municipality',                         'Ga-Segonyana',          'B', 'Ga-Segonyana Spatial Planning By-Law'),
    ('NC',  'Gamagara Local Municipality',                             'Gamagara',              'B', 'Gamagara Spatial Planning By-Law'),
    ('NC',  'Hantam Local Municipality',                               'Hantam',                'B', 'Hantam Spatial Planning By-Law'),
    ('NC',  'Joe Morolong Local Municipality',                         'Joe Morolong',          'B', 'Joe Morolong Spatial Planning By-Law'),
    ('NC',  'Kai !Garib Local Municipality',                           'Kai !Garib',            'B', 'Kai !Garib Spatial Planning By-Law'),
    ('NC',  'Kamiesberg Local Municipality',                           'Kamiesberg',            'B', 'Kamiesberg Spatial Planning By-Law'),
    ('NC',  'Kareeberg Local Municipality',                            'Kareeberg',             'B', 'Kareeberg Spatial Planning By-Law'),
    ('NC',  'Karoo Hoogland Local Municipality',                       'Karoo Hoogland',        'B', 'Karoo Hoogland Spatial Planning By-Law'),
    ('NC',  'Kgatelopele Local Municipality',                          'Kgatelopele',           'B', 'Kgatelopele Spatial Planning By-Law'),
    ('NC',  'Khai-Ma Local Municipality',                              'Khai-Ma',               'B', 'Khai-Ma Spatial Planning By-Law'),
    ('NC',  'Magareng Local Municipality',                             'Magareng',              'B', 'Magareng Spatial Planning By-Law'),
    ('NC',  'Nama Khoi Local Municipality',                            'Nama Khoi',             'B', 'Nama Khoi Spatial Planning By-Law'),
    ('NC',  'Phokwane Local Municipality',                             'Phokwane',              'B', 'Phokwane Spatial Planning By-Law'),
    ('NC',  'Renosterberg Local Municipality',                         'Renosterberg',          'B', 'Renosterberg Spatial Planning By-Law'),
    ('NC',  'Richtersveld Local Municipality',                         'Richtersveld',          'B', 'Richtersveld Spatial Planning By-Law'),
    ('NC',  'Siyancuma Local Municipality',                            'Siyancuma',             'B', 'Siyancuma Spatial Planning By-Law'),
    ('NC',  'Siyathemba Local Municipality',                           'Siyathemba',            'B', 'Siyathemba Spatial Planning By-Law'),
    ('NC',  'Sol Plaatje Local Municipality',                          'Sol Plaatje',           'B', 'Sol Plaatje Town Planning Scheme'),
    ('NC',  'Thembelihle Local Municipality',                          'Thembelihle',           'B', 'Thembelihle Spatial Planning By-Law'),
    ('NC',  'Tsantsabane Local Municipality',                          'Tsantsabane',           'B', 'Tsantsabane Spatial Planning By-Law'),
    ('NC',  'Ubuntu Local Municipality',                               'Ubuntu',                'B', 'Ubuntu Spatial Planning By-Law'),
    ('NC',  'Umsobomvu Local Municipality',                            'Umsobomvu',             'B', 'Umsobomvu Spatial Planning By-Law'),

    -- ── North West ───────────────────────────────────────────
    ('NW',  'City of Matlosana Local Municipality',                    'Matlosana',             'B', 'City of Matlosana Spatial Planning By-Law'),
    ('NW',  'Ditsobotla Local Municipality',                           'Ditsobotla',            'B', 'Ditsobotla Spatial Planning By-Law'),
    ('NW',  'Greater Taung Local Municipality',                        'Greater Taung',         'B', 'Greater Taung Spatial Planning By-Law'),
    ('NW',  'JB Marks Local Municipality',                             'JB Marks',              'B', 'JB Marks Spatial Planning By-Law'),
    ('NW',  'Kagisano-Molopo Local Municipality',                      'Kagisano-Molopo',       'B', 'Kagisano-Molopo Spatial Planning By-Law'),
    ('NW',  'Kgetlengrivier Local Municipality',                       'Kgetlengrivier',        'B', 'Kgetlengrivier Spatial Planning By-Law'),
    ('NW',  'Lekwa-Teemane Local Municipality',                        'Lekwa-Teemane',         'B', 'Lekwa-Teemane Spatial Planning By-Law'),
    ('NW',  'Madibeng Local Municipality',                             'Madibeng',              'B', 'Madibeng Local Municipality Spatial Planning By-Law'),
    ('NW',  'Mahikeng Local Municipality',                             'Mahikeng',              'B', 'Mahikeng Spatial Planning By-Law'),
    ('NW',  'Mamusa Local Municipality',                               'Mamusa',                'B', 'Mamusa Spatial Planning By-Law'),
    ('NW',  'Maquassi Hills Local Municipality',                       'Maquassi Hills',        'B', 'Maquassi Hills Spatial Planning By-Law'),
    ('NW',  'Moretele Local Municipality',                             'Moretele',              'B', 'Moretele Spatial Planning By-Law'),
    ('NW',  'Moses Kotane Local Municipality',                         'Moses Kotane',          'B', 'Moses Kotane Spatial Planning By-Law'),
    ('NW',  'Naledi Local Municipality',                               'Naledi',                'B', 'Naledi Spatial Planning By-Law'),
    ('NW',  'Ramotshere Moiloa Local Municipality',                    'Ramotshere Moiloa',     'B', 'Ramotshere Moiloa Spatial Planning By-Law'),
    ('NW',  'Ratlou Local Municipality',                               'Ratlou',                'B', 'Ratlou Spatial Planning By-Law'),
    ('NW',  'Rustenburg Local Municipality',                           'Rustenburg',            'B', 'Rustenburg Town Planning Scheme'),
    ('NW',  'Tswaing Local Municipality',                              'Tswaing',               'B', 'Tswaing Spatial Planning By-Law'),

    -- ── Western Cape ─────────────────────────────────────────
    ('WC',  'Beaufort West Local Municipality',                        'Beaufort West',         'B', 'Beaufort West Spatial Planning By-Law'),
    ('WC',  'Bergrivier Local Municipality',                           'Bergrivier',            'B', 'Bergrivier Spatial Planning By-Law'),
    ('WC',  'Bitou Local Municipality',                                'Bitou',                 'B', 'Bitou Spatial Planning By-Law'),
    ('WC',  'Breede Valley Local Municipality',                        'Breede Valley',         'B', 'Breede Valley Zoning Scheme'),
    ('WC',  'Cape Agulhas Local Municipality',                         'Cape Agulhas',          'B', 'Cape Agulhas Spatial Planning By-Law'),
    ('WC',  'Cederberg Local Municipality',                            'Cederberg',             'B', 'Cederberg Spatial Planning By-Law'),
    ('WC',  'Drakenstein Local Municipality',                          'Drakenstein',           'B', 'Drakenstein Integrated Zoning Scheme'),
    ('WC',  'George Local Municipality',                               'George',                'B', 'George Municipality Zoning Scheme'),
    ('WC',  'Hessequa Local Municipality',                             'Hessequa',              'B', 'Hessequa Spatial Planning By-Law'),
    ('WC',  'Kannaland Local Municipality',                            'Kannaland',             'B', 'Kannaland Spatial Planning By-Law'),
    ('WC',  'Knysna Local Municipality',                               'Knysna',                'B', 'Knysna Spatial Planning By-Law'),
    ('WC',  'Laingsburg Local Municipality',                           'Laingsburg',            'B', 'Laingsburg Spatial Planning By-Law'),
    ('WC',  'Langeberg Local Municipality',                            'Langeberg',             'B', 'Langeberg Spatial Planning By-Law'),
    ('WC',  'Matzikama Local Municipality',                            'Matzikama',             'B', 'Matzikama Spatial Planning By-Law'),
    ('WC',  'Mossel Bay Local Municipality',                           'Mossel Bay',            'B', 'Mossel Bay Municipality Zoning Scheme'),
    ('WC',  'Oudtshoorn Local Municipality',                           'Oudtshoorn',            'B', 'Oudtshoorn Spatial Planning By-Law'),
    ('WC',  'Overstrand Local Municipality',                           'Overstrand',            'B', 'Overstrand Spatial Planning By-Law'),
    ('WC',  'Prince Albert Local Municipality',                        'Prince Albert',         'B', 'Prince Albert Spatial Planning By-Law'),
    ('WC',  'Saldanha Bay Local Municipality',                         'Saldanha Bay',          'B', 'Saldanha Bay Spatial Planning By-Law'),
    ('WC',  'Stellenbosch Local Municipality',                         'Stellenbosch',          'B', 'Stellenbosch Municipality Zoning Scheme'),
    ('WC',  'Swartland Local Municipality',                            'Swartland',             'B', 'Swartland Zoning Scheme'),
    ('WC',  'Swellendam Local Municipality',                           'Swellendam',            'B', 'Swellendam Spatial Planning By-Law'),
    ('WC',  'Theewaterskloof Local Municipality',                      'Theewaterskloof',       'B', 'Theewaterskloof Spatial Planning By-Law'),
    ('WC',  'Witzenberg Local Municipality',                           'Witzenberg',            'B', 'Witzenberg Spatial Planning By-Law')

) as m (province_code, name, short_name, category, zoning_scheme)
on p.code = m.province_code
on conflict (name) do nothing;
