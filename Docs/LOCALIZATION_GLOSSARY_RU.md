# Abyssal Protocol — Russian Localization Glossary

This glossary is the canonical Russian localization guide for Abyssal Protocol.
Use it before editing `Languages/Russian/`, Russian DefInjected labels/descriptions, Keyed UI strings, or player-facing text in C#.

The goal is not literal English-to-Russian conversion. The goal is consistent, readable, RimWorld-style Russian that keeps the mod's techno-infernal, ritual-industrial identity.

## Priority rules

1. Existing corrected canonical names in this file win over ad-hoc machine translation.
2. Player-visible UI must be short and readable first; long lore belongs in descriptions, codex text, or tooltips.
3. Do not translate proper names syllable-by-syllable.
4. Keep family names consistent across items, recipes, UI cards, unlock text, research text, and boss/summon previews.
5. When a term is ambiguous, check the actual Def type first: weapon, pawn, recipe, hediff, module, ritual, resource, or UI key.

## Ground truth order for Russian localization

```text
1. User-provided current local archive.
2. Actual Def/XML/source usage in that archive.
3. This glossary.
4. Existing Russian files under Languages/Russian/.
5. Live GitHub and latest commits.
6. Older memory or previous chat wording.
```

If a Russian translation exists but conflicts with this glossary, prefer this glossary and update the localization consistently.

## Style baseline

Abyssal Protocol is not medieval fantasy hell. It is infernal post-singularity ritual industry.

Preferred tone:

```text
techno-infernal
ritual-industrial
hostile high-tech
forbidden engineering
machine-readable ritual language
abyssal bio-mechanical escalation
```

Avoid:

```text
generic fantasy demons
cute or playful terms
literal transliteration of English names
three-noun machine-translation chains
ungrammatical adjective/noun agreement
raw English UI labels in Russian mode
```

## Core world terms

| English | Canonical Russian | Notes |
| --- | --- | --- |
| Abyssal Protocol | Abyssal Protocol / Протокол Бездны | Mod title may stay English. In lore text use `Протокол Бездны`. |
| Abyss | Бездна | Do not use `абисс` or `преисподняя`. |
| Abyssal | бездненный / Бездны | Prefer natural Russian: `ядро Бездны`, `бездненный остаток`. |
| Infernal | инфернальный | Good for systems, machines, rituals, high-tech hell tone. |
| Dominion | Доминион | Keep as proper domain name. |
| Rift | Рифт / разлом | Use `Рифт-` for named equipment families; use `разлом` in lore/descriptions. |
| Rupture | Разрыв | `Archon of Rupture` = `Архонт Разрыва`. |
| Crown | Корона | Capitalize when it is a metaphysical authority/principle. |
| Herald | Вестник | Do not use `геральд`. |
| Gate | Врата | Use for ritual/lore gate names. |
| Sigil | сигила | Feminine: `эта сигила`, `сигилу`, `сигилы`. |
| Residue | остаток | Resource context: `бездненный остаток`. |
| Core | ядро | Do not use `кор`. |
| Shard | осколок | `осколок Короны`, `осколок Доминиона`. |
| Attunement | настройка | UI: `уровень настройки`, `настройка колонии`. |
| Communion | сопряжение | Forge UI: `инфернальное сопряжение`. |
| Manifestation | проявление | Summoning/boss arrival context. |
| Breach | пробой / разлом | `Seam Breach` = `пробой шва`; `Unstable Breach` = `нестабильный пробой`. |
| Seam | шов | `Dominion seam` = `шов Доминиона`. |
| Instability | нестабильность | Gameplay/UI mechanic. |
| Nexus | нексус | Lowercase unless part of a proper title. |
| Null | нулевой / Нуль- | `Null Priest` = `нулевой жрец`; weapon prefixes may use `нуль-`. |
| Vesper | Веспер | Keep as proper name. |
| Canticle | кантикль | Ritual-tech term; avoid `песнопение` for compact item names. |
| Aegis | Эгида | Capitalize when it is the named Saint/Aegis shield system. |
| Halo | ореол | Not `нимб`, unless explicitly angelic/religious tone is needed. |
| Verdict | вердикт | Late-tier judgment/law theme. |
| Lawwoven | сплетённый законом | Use carefully; for compact item names prefer `законоплетёный` only if readable. |

## Russian grammar rules

### Sigil is feminine

Correct:

```text
Сигила угольных гончих
сигила Реакторного Святого
эта сигила открывает ритуал
переработать сигилу
```

Wrong:

```text
угольный гончая сигила
сигил Реакторного Святого
этот сигила
```

### Requirement plural forms

Use a Russian plural-aware helper for numeric UI counts.

| Number pattern | Form | Examples |
| --- | --- | --- |
| 1, except 11 | требование | `1 требование`, `21 требование`, `101 требование` |
| 2-4, except 12-14 | требования | `2 требования`, `3 требования`, `24 требования` |
| 0, 5-20, 11-14, 25-30 | требований | `0 требований`, `5 требований`, `11 требований`, `30 требований` |

Do not use one static `требований` string for all counts.

### Main noun controls adjective gender

```text
винтовка -> плазменная винтовка
клинок -> рифт-клинок
сигила -> сигила угольных гончих
панцирь -> панцирь святого носителя
ядро -> пепельное ядро
модуль -> пепельный модуль
конденсатор -> пепельный конденсатор
```

### Avoid noun-chain machine translation

Bad:

```text
святой эгида панцирь
пеплосвязанный модуль-конденсатор
корона конденсатор модуль
забвение хоровой
```

Good:

```text
Панцирь святого носителя Эгиды
пепельный конденсатор
коронный конденсатор
Хор Забвения
```

## UI glossary — Forge and Summoning

| English | Canonical Russian |
| --- | --- |
| Abyssal Forge | Бездненная кузница |
| Infernal Communion Console | Консоль инфернального сопряжения |
| Abyssal Summoning Circle | Круг призыва Бездны |
| Summoning Console | Консоль призыва |
| Selected pattern | Выбранный шаблон |
| Pattern | шаблон |
| Forge core | ядро кузницы |
| Next milestones | Следующие вехи |
| Upcoming patterns | Ближайшие шаблоны |
| Production queue | Очередь производства |
| Add bill | Добавить задание |
| In queue | В очереди |
| Search | Поиск |
| Clear | Очистить |
| Needs resources | Не хватает ресурсов |
| Locked | Закрыто |
| Craftable | Доступно |
| All | Всё |
| Materials on map | Материалы на карте |
| Unlocked at X residue | Открывается при X остатка |
| Current track | Текущая полоса |
| Attunement level | Уровень настройки |
| Colony attunement | Настройка колонии |
| Reactor state | Состояние реактора |
| Online | онлайн |
| Offline | офлайн |
| Requirement | требование |
| Requirements | требования |
| Requirement count | Количество требований |
| Insufficient residue | Не хватает остатка |
| Next pattern | Следующий шаблон |
| Next attunement | Следующая настройка |
| Opened | Открыто |
| Available | Доступно |
| Unavailable | Недоступно |
| Research required | Требуется исследование |
| Sigil required | Требуется сигила |
| Boss trophy required | Требуется трофей босса |

## Forge categories and subcategories

| English | Canonical Russian |
| --- | --- |
| All | Всё |
| Forge Core | Ядро кузницы |
| Weapons | Оружие |
| Armor | Броня |
| Implants | Импланты |
| Ritual | Ритуал |
| Turret systems | Турельные системы |
| Residue | Остаток |
| Capacitor | Конденсатор |
| Stabilizer | Стабилизатор |
| Melee | Ближний бой |
| Ranged | Дальний бой |
| Nexus | Нексус |
| Materials | Материалы |
| Core | Ядро |
| Apparel | Снаряжение |
| Utility | Утилитарное |
| Boss | Босс |
| Horde | Орда |

## Resources and progression materials

| English | Canonical Russian | Notes |
| --- | --- | --- |
| Abyssal Residue | бездненный остаток | Primary resource. |
| Gilded Abyssal Residue | позолоченный бездненный остаток | Not `золотой остаток`. |
| Reinforced Abyssal Residue | укреплённый бездненный остаток | Plasteel/spacer residue recipe context. |
| Ashen Core | пепельное ядро | |
| Archon Core | ядро архонта | |
| Rupture Core | ядро Разрыва | |
| Crown Shard | осколок Короны | |
| Dominion Crown Shard | осколок короны Доминиона | |
| Horde Fragment | фрагмент орды | |
| Choir Resonance Core | резонансное ядро Хора | |
| Saint Condensation Cell | конденсационная ячейка святого | |
| Bound Sigil | связанная сигила | Generic progress key. |
| Warden Sigil | сигила Надзирателя | |
| Archon Sigil | сигила Архонта | |
| Dominion Sigil | сигила Доминиона | |
| Horde Gate Sigil | сигила врат орды | |
| Targeting Sigil | сигила наведения | |
| Unstable Breach Sigil | сигила нестабильного пробоя | |
| Ember Hound Sigil | сигила угольных гончих | User-corrected canonical form. |
| Choir Engine Sigil | сигила Хорового Двигателя | Entity/miniboss sigil. |
| Reactor Saint Sigil | сигила Реакторного Святого | |

## Enemies, pawns, and bosses

| English | Canonical Russian | Notes |
| --- | --- | --- |
| Rift Imp | рифт-имп | Lowercase in generic labels. |
| Ember Hound | угольная гончая | Use for pawn/enemy. |
| Hexgun Thrall | хексган-раб | Consistent with weaponized thrall tone. |
| Chain Zealot | цепной фанатик | |
| Rift Sniper | рифт-снайпер | |
| Null Priest | нулевой жрец | |
| Breach Brute | пробойный громила | |
| Siege Idol | осадный идол | |
| Harvester | Жнец | Capitalized if proper pawn label style is desired. |
| Gate Warden | Страж Врат | |
| Warden of Ash | Надзиратель Пепла | |
| Choir Engine | Хоровой Двигатель | Entity/miniboss, not weapon. |
| Archon Beast | Зверь-Архонт | |
| Reliquary Archon Beast | Реликварный Зверь-Архонт | |
| Archon of Rupture | Архонт Разрыва | |
| Infernal Reactor Saint | Инфернальный Реакторный Святой | Full boss title. |
| Reactor Saint | Реакторный Святой | Short boss title. |
| Halo Husk | ореольная оболочка | |
| Rift Sapper | рифт-сапёр | |
| Null Cantor | нулевой кантор | |
| Aortic Chain Harrower | аортальный цепной боронователь | If too awkward in UI, shorten to `аортальный цепной мучитель`. |

## Weapons and turret modules

Use a hyphen for `Рифт-` equipment names. It reads better in Russian UI and prevents raw transliteration like `Рифт Бладе`.

| English | Canonical Russian | Notes |
| --- | --- | --- |
| Rift Blade | Рифт-клинок | User-corrected. Do not use `Рифт Бладе`. |
| Rift Carbine | Рифт-карабин | User-corrected. Do not use `Рифт Карбине`. |
| Rift Dagger | Рифт-кинжал | |
| Rift Helm | рифт-шлем | Apparel, but same family rule. |
| Rift Needler | рифт-игломёт | |
| Rift Needler Core | ядро рифт-игломёта | Turret module. |
| Rift Flak Bloom | рифт-осколочный цветок | Turret module / projectile family. |
| Ultra Plasma Rifle | ультра плазменная винтовка | User-corrected. No hyphen after `ультра`. |
| Specter Lash | призрачная плеть | |
| Specter Lash Projector | проектор призрачной плети | |
| Vesper Lance | Веспер-копьё | Proper weapon name. |
| Vesper Lance Array | массив Веспер-копья | Turret module. |
| Ashen Pike | пепельная пика | |
| Ashen Scattergun | пепельный дробовик | |
| Breach Cannon | пробойная пушка | |
| Gatebreaker Spiker | шипомёт Проломщика Врат | |
| Litany Grinder | жернова Литании | |
| Phalanx Driver | фаланговый пробиватель | |
| Sigil Repeater | сигильный повторитель | |
| Anchor Spiker | якорный шипомёт | |
| Aortic Chain Harrower | аортальный цепной боронователь | Weapon/pawn context must be checked. |
| Ash Choir Launcher | пускатель Пепельного Хора | |
| Canticle Driver | кантикль-пробиватель | |
| Null-Arc Discharger | нуль-дуговой разрядник | |
| Null Marksman Rifle | нулевая стрелковая винтовка | |
| Nullbrand Glaive | глефа Нулевого Клейма | |
| Crowncoil Gauss Minigun | гаусс-миниган Коронной Катушки | |
| Crownfire Rocket Choir | ракетный Хор Коронного Пламени | Turret weapon/module. |
| Crownshard Stormcaster | буревестник осколков Короны | |
| Crownspike Rail | рельсотрон Коронного Шипа | |
| Crown Interdictor | Коронный интердиктор | T4 target-lock melee weapon. |
| Abyssal Harpoon Projector | бездненный гарпунный проектор | |
| Ash Choir Repeater Core | ядро повторителя Пепельного Хора | Turret module. |
| Choir Arc Emitter | дуговой излучатель Хора | Turret module. |
| Plasma Lance Core | ядро плазменного копья | Turret module. |
| Sepulcher Rail Core | ядро гробничного рельсотрона | |
| Cinder Mortar Core | ядро угольной мортиры | |
| Sanctified Prism Emitter | освящённый призменный излучатель | |
| Oblivion Choir | Хор Забвения | Weapon/proper name. Do not classify as pawn/enemy. |

### Oblivion Choir special rule

`Oblivion Choir` is a weapon name in the current naming plan. It must stay in the weapon section.

```text
Oblivion Choir
Canonical RU: Хор Забвения
Category: weapon
Do not translate as: Забвение хоровой, Хоровое Забвение, Хор Обливиона
Notes: Treat as a named abyssal weapon, not as an enemy, faction, or unit.
```

## Armor and apparel

| English | Canonical Russian | Notes |
| --- | --- | --- |
| Infernal Combat Frame | инфернальный боевой каркас | |
| Rift Helm | рифт-шлем | |
| Rift Conduit Gloves | рифт-проводящие перчатки | |
| Rift Vector Vambraces | рифт-векторные наручи | |
| Riftstep Greaves | рифт-ступающие поножи | |
| Rift Relay Pack | рифт-релейный ранец | |
| Gatebreaker Helm | шлем Проломщика Врат | |
| Gatebreaker Carapace | панцирь Проломщика Врат | |
| Gatebreaker Anchor Harness | якорная сбруя Проломщика Врат | |
| Saint Aegis Carapace | Панцирь святого носителя Эгиды | User-corrected canonical form. |
| Crowned Core Helm | шлем Коронованного Ядра | |
| Crowned Core Plate | плита Коронованного Ядра | |
| Crown Authority Vambraces | наручи коронной власти | |
| Crown Conduit Pack | коронный проводящий ранец | |
| Crownpath Sabatons | сабатоны Коронного Пути | |
| Crownseal Gauntlets | латные перчатки Коронной Печати | |
| Ashbound Field Pack | пеплосвязанный полевой ранец | Acceptable for apparel; use shorter forms in cramped UI. |
| Ashbound Tread Boots | пеплосвязанные походные ботинки | |
| Ashen Grip Gloves | пепельные хватательные перчатки | |
| Ashen Vambraces | пепельные наручи | |
| Vesper Halo Helm | шлем Веспер-ореола | |
| Null Acolyte Cowl | капюшон нулевого аколита | |
| Null Acolyte Vestment | облачение нулевого аколита | |
| Null Procession Veil | вуаль Нулевой Процессии | |
| Dominion Gravplate Shell | гравипанцирь Доминиона | T5 grav-hover armor shell; keep compact for UI. |
| Dominion Gravplate Helm | гравишлем Доминиона | Matching T5 gravplate helm. |
| Lawwoven Carapace Mesh | панцирная сеть, сплетённая законом | For compact label: `законоплетёная панцирная сеть` if needed. |

## Circle modules, capacitors, stabilizers

| English | Canonical Russian | Notes |
| --- | --- | --- |
| Residue Capacitor | конденсатор остатка | Turret/module context. |
| Rift Capacitor Module | рифт-конденсатор | Short UI label. |
| Ashbound Capacitor Module | пепельный конденсатор | User-reported UI fix; avoid cramped `пеплосвязанный модуль-конденсатор`. |
| Crown Condenser Module | коронный конденсатор | |
| Crude Stabilizer Module | грубый стабилизатор | |
| Resonant Stabilizer Module | резонансный стабилизатор | |
| Crown Stabilizer Module | коронный стабилизатор | |
| Circle Stabilizer Frames | каркасы стабилизаторов круга | Research/progression. |
| Crown Condensation Arrays | коронные конденсационные массивы | Research/progression. |
| Cooling Lattice | охлаждающая решётка | Turret module. |
| Targeting Sigil | сигила наведения | Turret module. |

## Implants and Hediffs

| English | Canonical Russian |
| --- | --- |
| Abyssal Attunement | бездненная настройка |
| Infernal Eye | инфернальный глаз |
| Herald Eye | глаз Вестника |
| Ember Lung Array | угольный лёгочный массив |
| Rift Heart | рифт-сердце |
| Saint Reactor Heart | реакторное сердце святого |
| Aegis Sink Kidney | почка-поглотитель Эгиды |
| Choir Sink Kidney | почка-поглотитель Хора |
| Crown Cortex Subnode | подузел коронной коры |
| Dominion Pulse Heart | пульсирующее сердце Доминиона |
| Halo Subcore Node | подъядерный узел ореола |
| Null Chorus Collar | ошейник Нулевого Хора |
| Vesper Servo Arm | Веспер-серворука |
| Archon Tendon Spine | сухожильный позвоночник архонта |
| Ashen Anchor Node | пепельный якорный узел |
| Ashen Liver Lattice | пепельная печёночная решётка |
| Cinder Mandible Seal | угольная мандибулярная печать |
| Canticle Subcore Node | подъядерный узел кантикля |
| Resonance Servo Arm | резонансная серворука |
| Harmonic Mesh | гармоническая сеть |
| Cohort Sync Subnode | подузел синхронизации когорты |
| Bound Claw Array | массив связанных когтей |
| Breach Tendon Weave | сухожильное плетение пробоя |
| Verdict Tendon Spine | сухожильный позвоночник Вердикта |
| Herald Carapace Mesh | панцирная сеть Вестника |

## Research and Protocol Nexus terms

| English | Canonical Russian |
| --- | --- |
| Abyssal Signal Theory | теория бездненного сигнала |
| Abyssal Signal Interpretation | интерпретация бездненного сигнала |
| Primitive Breach Protocols | примитивные протоколы пробоя |
| Residue Recognition | распознавание остатка |
| Residue Processing | переработка остатка |
| Forge Contact Vocabulary | словарь контакта кузницы |
| Ashbound Capacitance | пепельная ёмкость |
| Rift Capacitance Matrices | рифт-ёмкостные матрицы |
| Basic Abyssal Arms | базовое бездненное вооружение |
| Ashbound Combat Kit | пеплосвязанный боевой комплект |
| Rift Ballistics | рифт-баллистика |
| Null Geometry Handling | обращение с нулевой геометрией |
| Basic Abyssal Implants | базовые бездненные импланты |
| Abyssal Armor Systems | бездненные бронесистемы |
| Elite Summoning Patterns | шаблоны элитного призыва |
| Core Integration | интеграция ядер |
| Modular Turret Interface | интерфейс модульных турелей |
| Archon Sigil Handling | обращение с сигилой Архонта |
| Major Boss Invocation | воззвание к старшему боссу |
| Crown Logic Decoding | расшифровка логики Короны |
| Heavy Infernal Systems | тяжёлые инфернальные системы |
| Advanced Implants | продвинутые импланты |
| Gatebreaker Carapace Logic | логика панциря Проломщика Врат |
| Breach Lockdown Systems | системы блокировки пробоя |
| Dominion Gate Bootstrapping | начальная развёртка врат Доминиона |
| Dominion Slice Cartography | картография среза Доминиона |
| Infernal Pocket Stabilization | стабилизация инфернального кармана |
| Anchor-Law Breach Analysis | анализ пробоя якорного закона |
| Dominion Survival Frames | каркасы выживания Доминиона |
| Crowned Core Extraction | извлечение Коронованного Ядра |
| Dominion Biology | биология Доминиона |
| Final Gate Architecture | архитектура Финальных Врат |
| Crowned Gate Invocation | воззвание к Коронованным Вратам |
| Apex Weaponry | вершинное вооружение |
| Crownfire / Sepulcher Calibration | калибровка Коронного Пламени / Гробницы |
| Heraldic Fragment Analysis | анализ фрагментов Вестника |
| Oblivion Choir Interface | интерфейс Хора Забвения |
| Schism Cartography / Godshard Forensics | картография Раскола / экспертиза осколков бога |
| Interface Beyond the Gate | интерфейс за Вратами |

## Events, manifestations, and Dominion

| English | Canonical Russian |
| --- | --- |
| Sigil Bloom Manifestation | проявление цветения сигилы |
| Static Phase-In Manifestation | проявление статического фазового входа |
| Seam Breach Manifestation | проявление пробоя шва |
| Archon Beast Manifestation | проявление Зверя-Архонта |
| Reactor Saint Manifestation | проявление Реакторного Святого |
| Reactor Saint Cocoon | кокон Реакторного Святого |
| Dominion Slice | срез Доминиона |
| Dominion Pocket | карман Доминиона |
| Dominion Seam Emergence | выход через шов Доминиона |
| Dominion Collapse | коллапс Доминиона |
| Aegis Collapse | коллапс Эгиды |
| Reactor Saturation | насыщение реактора |
| Plasma Destabilization | плазменная дестабилизация |
| Dominion Extraction Gate | эвакуационные врата Доминиона |
| Great Infernal Gate | великие инфернальные врата |
| Suppression Anchor | якорь подавления |
| Drain Anchor | якорь истощения |
| Ward Anchor | защитный якорь |
| Breach Anchor | якорь пробоя |
| Dominion Ruin Wall Segment | сегмент руинной стены Доминиона |
| Broken Dominion Ruin Wall | разрушенная руинная стена Доминиона |
| Dominion Perimeter Barricade | периметровая баррикада Доминиона |
| Dominion Industrial Wreckage | индустриальные обломки Доминиона |
| Dominion Seam Emergence Scar | шрам выхода через шов Доминиона |
| Dominion Edge Void Tear | краевой разрыв пустоты Доминиона |

## Difficulty and boss UI

| English | Canonical Russian |
| --- | --- |
| Normal | обычная |
| Severe | тяжёлая |
| Rupture | Разрыв |
| Dominion | Доминион |
| Final Gate | Финальные Врата |
| Aegis | Эгида |
| Aegis collapse | коллапс Эгиды |
| Phase | фаза |
| Boss health | здоровье босса |
| Shield | щит |
| Reactor aegis | реакторная Эгида |
| Adjust boss bar | настроить панель босса |

## Banned or corrected translations

| Do not use | Use |
| --- | --- |
| Рифт Бладе | Рифт-клинок |
| Рифт Карбине | Рифт-карабин |
| Ултра-плазменный винтовка | ультра плазменная винтовка |
| Забвение хоровой | Хор Забвения |
| Хоровое Забвение | Хор Забвения |
| Хор Обливиона | Хор Забвения |
| Святой эгида панцирь | Панцирь святого носителя Эгиды |
| угольный гончая сигила | Сигила угольных гончих |
| Уровень настройка | Уровень настройки |
| Selected pattern | Выбранный шаблон |
| Needs resources | Не хватает ресурсов |
| Requirement count | Количество требований |
| 1 требований | 1 требование |
| 2 требований | 2 требования |
| 21 требований | 21 требование |
| пеплосвязанный модуль-конденсатор | пепельный конденсатор |

## How to localize new entries

Before adding a Russian string:

```text
1. Identify the Def type and gameplay category.
2. Check whether the English term already appears in this glossary.
3. Reuse the canonical family term: Рифт-, Корона, Доминион, Эгида, Хор, Вестник, сигила.
4. Check adjective gender and noun case.
5. For UI cards/buttons, prefer compact labels.
6. For descriptions, use full lore style but keep the sentence readable.
7. Scan for raw English and Latin-only values after editing.
8. Scan for duplicate flat language keys and orphan DefInjected keys.
9. If a new recurring family term is introduced, add it to this glossary in the same patch.
```

## Compact UI vs long description examples

| Context | Preferred Russian |
| --- | --- |
| Forge card label | `пепельный конденсатор` |
| Full description | `Модуль-конденсатор, удерживающий ритуальное давление в пепельной проводимости круга.` |
| Forge card label | `Рифт-карабин` |
| Full description | `Автоматический рифт-карабин, рассчитанный на среднюю дистанцию и пробивание нестабильной брони.` |
| Boss UI | `Эгида` |
| Tooltip/lore | `Реакторная Эгида удерживает святого в фазе боевой неуязвимости, пока её цепи не разрушены.` |

## 2026-05-19 editorial additions

These forms are preferred after the glossary-driven Russian editorial pass. Use short forms in Forge cards and recipe rows; keep full lore forms for descriptions/tooltips.

| English / Source term | Compact Russian | Notes |
| --- | --- | --- |
| Ashbound Capacitor Module | пепельный конденсатор | Avoid `пеплосвязанный модуль-конденсатор` in tight UI. |
| Rift Capacitor Module | рифт-конденсатор | Compact Forge/browser label. |
| Crown Condenser Module | коронный конденсатор | Compact Forge/browser label. |
| Crude Stabilizer Module | грубый стабилизатор | Compact Forge/browser label. |
| Resonant Stabilizer Module | резонансный стабилизатор | Compact Forge/browser label. |
| Crown Stabilizer Module | коронный стабилизатор | Compact Forge/browser label. |
| Null Procession Veil | вуаль Нулевой Процессии | Do not use `нулевой процессия веил`. |
| Null Cantor Focus Sling | перевязь нулевого кантора | Recipe labels may shorten to `перевязь кантора`. |
| Gatebreaker Anchor Harness | якорная сбруя Проломщика Врат | Do not transliterate `harness`. |
| Dominion Gate / Great Hell Gate legacy text | Великие инфернальные врата | Do not use `Великий адский портал`. |
| Reward cache | тайник / тайники | Avoid `кэш/кэши` in Russian UI. |
| Pipeline | цепочка | Avoid raw `пайплайн` in player-facing Russian. |
| UI scanline/sweep | сканирующие линии / скользящие акценты | Avoid raw English terms in Russian mode. |


## Maintenance rule

Update this glossary when:

```text
- a new named weapon family is added;
- a new enemy/boss family is added;
- a term is corrected by in-game screenshot review;
- a recurring Russian translation error is found;
- UI code introduces a new category/subcategory/status chip;
- a new research/protocol category creates stable terminology.
```

Do not silently override this glossary in `Languages/Russian/`. If the glossary is wrong, update the glossary and the localization together.
