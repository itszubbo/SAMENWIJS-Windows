# SAMENWIJS voor Windows

**Duidelijk wat komt. Ruimte om zelf te kiezen.**

SAMENWIJS is een Nederlandstalige daghulp voor volwassenen met een licht verstandelijke beperking (LVB), autisme, of allebei. De gebruiker houdt zelf de regie: onderdelen zijn optioneel, informatie is aanpasbaar en delen gebeurt pas na een bewuste keuze.

## Windows-versie

De eerste desktopversie is bedoeld voor Windows 10 en 11 op x64. De installer zet SAMENWIJS in de persoonlijke Windows-map van de gebruiker en maakt een snelkoppeling op het bureaublad en in het Startmenu. De app toont bij het starten een eigen splashscreen en heeft een eigen pictogram en launcher.

De MSI-installatie maakt snelkoppelingen op het bureaublad en in het Startmenu. De app heeft Microsofts WebView2 Runtime nodig. Als die ontbreekt, meldt SAMENWIJS dit en verwijst het programma naar Microsofts downloadpagina. De runtime zit niet in de MSI.

## Functies

- **Vandaag en Mijn dag:** activiteiten voor een gekozen dag bekijken, toevoegen, wijzigen, afvinken en naar een andere dag kopiëren.
- **Seintjes:** zelf een herinnering instellen voor een activiteit. De app kan deze opnieuw inplannen als hij opnieuw wordt geopend. Een seintje verschijnt zolang SAMENWIJS actief is, ook wanneer het in het systeemvak staat.
- **Vaardigheden:** eenvoudige stappenplannen voor onder meer zelfzorg, eten, thuis, afspraken, onderweg, contact, geld en veiligheid. De gebruiker kan eigen plannen en doelen toevoegen.
- **Mijn geld:** een zelfgekozen maandbedrag en handmatige uitgaven bijhouden. Dit is geen bankkoppeling en voert geen betalingen uit.
- **Rust:** gevoelens en prikkels noteren, een persoonlijk rustplan maken, een ademhalingsklok gebruiken en een korte grondoefening doen.
- **Samen:** een persoonlijke hulpkaart, contactpersonen, communicatiezinnen en uitleg voor ondersteuners.
- **Toegankelijkheid:** extra grote tekst, hoog contrast, donkere kleuren, een eenvoudige startpagina en voorlezen met een Nederlandse Windows-stem als die is geïnstalleerd.
- **Gegevensbeheer:** gegevens opslaan als bestand of alle gegevens wissen.

## Installeren

Gebruik het MSI-bestand `SAMENWIJS-Setup.msi`. Na de installatie vind je SAMENWIJS in het Startmenu en op het bureaublad. De installatie is per gebruiker; administratorrechten zijn normaal gesproken niet nodig.

## Zelf bouwen

Benodigd op een Windows-computer:

- .NET 10 SDK voor Windows
- WiX Toolset CLI 5.0.2
- WebView2 Runtime om de app te starten

Installeer WiX indien nodig en bouw daarna de MSI vanuit PowerShell:

```powershell
dotnet tool install --global wix --version 5.0.2
.\windows\build-windows.ps1
```

De installer komt in `windows/artifacts/SAMENWIJS-Setup.msi`. Bij elke push, pull request of handmatige Windows-build maakt GitHub Actions ook een MSI als downloadbaar workflow-artifact.

## Privacy en grenzen

SAMENWIJS heeft geen account, advertenties, analytics, cloudopslag of automatische gegevensdeling. Dagplannen, gevoelens, doelen, budget en contactgegevens blijven lokaal op de computer. Een hulpkaart delen of gegevens exporteren begint pas na een keuze van de gebruiker. De telefoonknop opent alleen een ingestelde Windows-belapp; als dat niet lukt, kan het nummer worden gekopieerd.

SAMENWIJS geeft geen medisch of financieel advies en vervangt geen begeleiding. De app is een eerste werkende basis, geen gevalideerd zorgproduct. Begrijpelijkheid, toegankelijkheid, stappenplannen en veiligheidsinformatie moeten met mensen met LVB en autisme en hun ondersteuners worden besproken en verbeterd voordat SAMENWIJS breder wordt ingezet.

## Android

Android is uitgesteld. Deze Windows-repository bevat geen Android-build. Android staat op de planning voor later.
