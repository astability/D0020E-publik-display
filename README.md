# Publik-display

Det här är ett projekt som gick ut på att utveckla en display
som skulle visa status på kopplade system samt ett bildspel för
besökare.

Om du läser den här filen så är du förmodligen här för en av
två anledningar:

 1) Du ska försöka att faktiskt köra den här programmet.
 2) Du är med i en grupp som ska utveckla ett nytt system som gör
    samma sak.
    
Den här READMEN kommer gå igenom dessa rubriker i ordning.

## 1. Installation
Programmet är byggts för att visa en webbsida som visas på en browser
som har sätts i "kiosk mode", det vill säga så att den bara kan visa
webbsidan som programmet hostar. Programmet förutsätter att MongoDB
kör tillsammans med programmet på localhost port 27017 (standardport
för MongoDB). Programmet är byggt i .NET 5.0, men är någorlunda portabelt.
Denna guide kommer förutsätta att allt körs på Windows.

Notera att koden har bara testats i Visual Studio (notera inte Visual
Studio code) och aldrig körts på faktisk skärm.

### 1.1. Installation av MongoDB
Från
https://stackoverflow.com/questions/20796714/how-do-i-start-mongo-db-from-windows

 1) Installera MongoDB Community för ditt operativsystem. Instruktioner
    för detta kan hittas här:
    https://docs.mongodb.com/manual/administration/install-community/

 2) Skapa mappen där MongoDB sparar data, `C:\data\db`

 3) Starta MongoDB i genom att öppna platsen där MongoDB har installerats
    i komandoraden och kör `mongod`.
    Detta är vanligtvis under
    `C:\Program Files\MongoDB\Server\<version>\bin\` där `<version>` är
    versionen av MongoDB.

### 1.2. Viktiga collections för databas

Programmet förutsätter att MongoDB har en databas med namn `display` som
har en collection `systems`. JSON-filer för `systems` samt
collections för systemen som `systems` definerar kan hittas i mappen
DbCollections. Notera att collections som slutar med `devices` eller
`logs` kan lämnas tomma.

MongoDB Compass kan underlätta att lägga till collections.
https://www.mongodb.com/products/compass

### 1.3. Kör koden
Projektet måste öppnas i Visual Studio (inte Visual Studio code) för
att kunna köras. Inga försök att köra koden utanför IDE har inte
försökts.

För att testa systemet finns följande admin-konto:
__Användarnamn:__`testLogin`
__Lösenord:__`admin`

## 2. Tips för vidareutveckling
Här är ett antal allmänna tips för hur ett projekt för att vidarutveckla
den här koden eller ett helt nytt projekt skulle kunna göras.

### 2.1. Allmän projektledning
Detta är ett projekt där där svårt att förutsäga hur lång tid en uppgift
kommer ta. Så allmänt har vi två tips:

 1) Börja så tidigt som möjligt. Det kanske verkar självklart, men undvik
    att vänta på saker. Om det går att arbeta på projektet, gör det.
    Planera gärna så att alla i gruppen kan jobba parallelt på mindre
    delar. Det enda som man bör vänta på är ordentlig planering av
    projektet eller om inget verkligen kan göras.
    
 2) Planera ordentligt och följ planerna. För en agil grupp av 4 är det
    ytterst viktigt att alla har en task att göra och att ni kan lätt
    hantera dem. Detta kan göras genom verktyg som Trello eller Github
    issues.
    
### 2.2. Databas och MongoDB
Systemet använder MongoDB som databas, vilket är "modellen" i systemet.
Frontend-sidan hämtar datan från den och backend skriver till den.
Eftersom den är så central är formatet av databasen något som bör
planeras ordentligt i detalj innan den integreras i projektet.

MongoDB är relativt enkel att använda, men en relationell databas kommer
fungera exakt lika bra och förmodligen bättre för det här projektet.
Ett problem med MongoDB är att medans modifikationer till ett dokument
är atomiska, stöds inte större transaktioner om databasen inte är
replikerad eller shardad.

Ett dokument med databasformatet finns bifogad (`mongodb format.pdf`)

### 2.3. Arkitektur
Systemet följer MVC-modellen där frontend är view, databas är modellen,
och en upsättning av s.k. "Monitors" som är kontrollern.

Varje monitor är ett objekt som kontinuerligt läser in data från
sensorerna och skriver det till databasen efter det har
instansierats. Objektorienteringen är praktisk här eftersom det finns
dubletter av vissa system som kan monitoreras med ett nytt objekt.

Monitorer får in sin data med ett system-specifikt "Reader"-objekt som
parsar datan till ett lätthanterligt format.
Tanken här är att objektet bytas ut med polymorfism med ett falskt
objekt som pytsar ut förinspelad data för att kunna testa utan kontakt
med systemet eller med systemproblem som det riktiga systemet sällan
har.

### 2.4. Allmänna koncept
Nästintill alla system verkar kunna hanteras så här:

 * Ett *System* har en mängd kopplade *Enheter* som kan ha pågående
   *Konditioner* (problem, t.ex. enhet svarar inte) systemet kan själv
   också ha pågående *Konditioner* (t.ex. kan inte koppla till server).
   
 * En *Kondition* har en *Typ* och *Allvarlighetsgrad*, som beror på
   *Typen*. En *Kondition* är pågående med en start och början som bör 
   hanteras som *Händelser*.

 * En *Händelse* (loggmeddelande) har *Typ* och *Allvarlighetsgrad* i
   samma mån som en *Kondition*, men har en exakt stund där den har hänt.

