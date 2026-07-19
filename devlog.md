# Devlog — InteractiveTimetable

Projektplanungstool (WPF, .NET 10), ersetzt den Excel-Projektzeitplan (`..\CurrentState.JPG`).
Architektur- und Konzeptentscheidungen: siehe Memory bzw. unten „Grundentscheidungen".

## Grundentscheidungen

- Clean Architecture: `Timetable.Domain` / `Timetable.Application` / `Timetable.Infrastructure` / `Timetable.App` (WPF), MVVM mit CommunityToolkit.Mvvm, Tests mit xUnit + NSubstitute (test-first in der Domäne).
- Mehrbenutzer bewusst lightweight: eine gemeinsame JSON-Datei auf einem Share, Anwesenheit über Sidecar-Heartbeat-Dateien (`FilePresenceService`), Konflikterkennung über Versions-Token beim Speichern („Letzter gewinnt" nur nach expliziter Warnung, mit automatischem Backup), rotierende Backups. Kein Server.
- Unscharfe Termine: `FuzzyDate` (tages-/wochen-/monatsgenau, Excel-Schreibweise „xx.03.2027"), KW-Raster nach ISO 8601 (`IsoWeek`, Jahreswechsel KW 53→1).
- Dialog-Muster in der App: Editor-ViewModel hält eine Bearbeitungskopie, `Validate()` liefert Fehlermeldung oder null, `ApplyTo(...)` schreibt erst bei OK in die Domäne zurück; Dialoge sind schlanke Code-Behind-Fenster mit MessageBox für Validierung/Rückfragen.

## Erledigt

### Phase 1 — Excel ersetzen (komplett, Stand 19.07.2026)

- **Domäne:** `ProjectPlan` (Wurzel), `Project`, `Milestone` (+ `TaskItem` für Phase 2), `Person`, `HolidayPeriod`, `ExternalEvent`, `FuzzyDate`, `IsoWeek`.
- **Persistenz:** `JsonPlanRepository` (JSON-Datei, Versions-Token, `PlanConflictException`, Backups), `FuzzyDateJsonConverter`.
- **Kollaboration:** `FilePresenceService` (Heartbeat-Sidecar-Dateien), Anzeige „Ebenfalls geöffnet: …" in der Statusleiste (15-s-Timer).
- **Timeline:** `TimelineBuilder`/`TimelineModel` in der Application-Schicht (reine Daten), WPF zeichnet nur noch (`TimelineHeaderControl`, `TimelineRowsControl`); Monats-/KW-Kopf, Ferien-/Event-Bänder, Heute-Spalte, Statusfarben.
- **Hauptfenster:** Neu/Öffnen/Speichern/Speichern unter, zuletzt geöffnete Datei wird beim Start wieder geöffnet (`SettingsStore`), Speicherkonflikt-Dialog, eingefrorene Kopf-/Tabellenbereiche mit synchronisiertem Scrolling.
- **Bearbeitungsdialoge** (alle nach dem o. g. Muster):
  - `MilestoneDialog` + `MilestoneEditorViewModel` — Titel, Status, FuzzyDate-Termin, Lead, Beteiligte, Anmerkung; Löschen mit Rückfrage. Doppelklick auf Meilensteinzeile = bearbeiten, auf Projektzeile = neuer Meilenstein (links wie rechts auf der Zeitachse).
  - `ProjectDialog` + `ProjectEditorViewModel` — Name, Lead, Beteiligte; Löschen mit Rückfrage (nennt Anzahl der Meilensteine). Toolbar „Neues Projekt…", Rechtsklick-Kontextmenü auf Projektzeile („Projekt bearbeiten…", „Neuer Meilenstein…") und Meilensteinzeile („Meilenstein bearbeiten…").
  - `PeopleDialog` + `PeopleEditorViewModel` — DataGrid mit Kürzel/Name, Hinzufügen/Entfernen; Kürzel pflichtig und eindeutig; beim Entfernen Rückfrage mit Referenzanzahl, `ApplyTo` bereinigt alle Verweise (Projekt-/Meilenstein-Lead und -Beteiligte, Task-Zuordnungen). Toolbar „Personen…".
  - `PlanInfoDialog` + `PlanInfoEditorViewModel` — Titel + „Stand"-Datum. Toolbar „Plandaten…".
  - `CalendarDialog` + `CalendarEditorViewModel` — zwei Tabs (Ferien / Externe Events), DataGrid mit Name/Von/Bis (Text, TT.MM.JJJJ), Events zusätzlich „Hervorheben"; Validierung inkl. Bis ≥ Von. Toolbar „Ferien & Events…".
- Gemeinsame Bausteine: `LeadChoice.ListFor(plan)` (Lead-Auswahlliste), `PersonSelection` (Beteiligten-Checkboxen).

### Phase 2 — Wochenansicht (umgesetzt, Stand 19.07.2026)

- **Application:** `WeekModel`/`WeekBuilder` in `Timetable.Application.Timeline` (analog `TimelineModel`/`TimelineBuilder`): 7 Tagesspalten (`DayColumn`, Wochenende markiert), Ferien/Events als `DayBandSegment` auf Tage beschnitten, Zeilen `WeekProjectRow`/`WeekMilestoneRow` (Marker, wenn der Fuzzy-Termin die Woche berührt)/`WeekTaskRow` (auf die Woche beschnitten, `ContinuesBefore/After`-Flags). Personen-Kürzel über gemeinsames `PersonLabels` (auch von `TimelineBuilder` genutzt). Tests: `WeekBuilderTests`.
- **Controls:** `WeekHeaderControl` (Tage + Ferien-/Eventbänder, gleiche Gesamthöhe 76 wie der Jahres-Kopf), `WeekRowsControl` (Tagesraster, Wochenend-Schattierung, Heute-Spalte, Aufgabenbalken in Statusfarbe mit Kürzeln, an der Wochenkante „abgeschnitten" wenn die Aufgabe weiterläuft). Maße in `TimelineMetrics` (`DayWidth`, `DayRowHeight`).
- **Hauptfenster:** ToggleButton „Wochenansicht" (`IsWeekView`), Navigation ◀/Heute/▶ + `WeekTitle` (nur in Wochenansicht sichtbar), zwei parallele Grids mit Visibility-Umschaltung; Scroll-Sync wie in der Jahresansicht.
- **Bedienung Wochenansicht:** Doppelklick auf Meilensteinzeile = neue Aufgabe (Vorbelegung Mo–Fr der angezeigten Woche), auf Aufgabenzeile = bearbeiten, auf Projektzeile = neuer Meilenstein; Kontextmenüs entsprechend („Neue Aufgabe…", „Aufgabe bearbeiten…", …).
- **`TaskDialog` + `TaskEditorViewModel`** — Titel, Status, Von/Bis (DatePicker), Zugeordnete; Löschen mit Rückfrage.
- Demodaten enthalten Beispiel-Aufgaben (u. a. in KW 29–31/2026, um die Ansicht direkt zu füllen).

**Hinweis:** `PlanInfoDialog`, `CalendarDialog` und die gesamte Wochenansicht sind noch nicht build-geprüft — der User baut selbst. `DemoData.CreatePlan()` ist weiterhin der Startzustand, solange keine Datei geöffnet ist.

## Offen

### Phase 3 — Komfort
- Excel-Export mit ClosedXML (Layout wie `CurrentState.JPG`).
- Ferien-API (Schulferien automatisch beziehen) — Andockpunkt: `CalendarEditorViewModel`/`Plan.Holidays`.
- Druckansicht.

### Kleinere bekannte Punkte
- Demodaten: sollte es „Neu" wirklich mit leerem Plan starten? Aktuell startet die App ohne Datei mit Demodaten.
- Kein Undo/Redo — Sicherheitsnetz ist bisher nur „Abbrechen" in den Dialogen und die Backups beim Speichern.
- Keine Tests für die App-Schicht (ViewModels); Domäne/Application/Infrastructure sind getestet.
- Die Ansicht (Jahr/Woche, gewählte KW) wird nicht in den Einstellungen gemerkt — Start immer in der Jahresansicht mit der aktuellen KW.
