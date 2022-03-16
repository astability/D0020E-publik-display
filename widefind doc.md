# Widefind över MQTT

Widefind kan få tas på över MQTT från sin broker (på 130.240.74.55:1883) under topic `ltu-system/#`.
Vid uppkoppling så skickas meddelandet `test message` ett par gånger innan JSON börjas sändas. 

Dessa meddelanden ser generellt ut så här, där `message` är generellt sett det enda relevanta
fältet vi behöver tolka för att få all relevant information. (utom möjligtvis `time`)
```json
{"host":"WFGATEWAY-3ABFF8D01EFF","message":"BEACON:9691FE799F371A4C,0.2.7,5190,-5820,2500,4.11,-91.6,655829,MAN,SAT*996A","source":"03FF5C0A2BFA3A9B","time":"2022-02-01T14:34:11.228484322Z","type":"widefind_message"}
```

Alla `message`-strängar har formatet
```
<type>:<csv>*<unknown>
```
Där 
>`<type>` representerar typen av meddelandet.

>`csv` är ett antal kommaseparerade värden som är parametrar för händelsen.

> `<unknown>` verkar vara ett slumpat 16-bittars hexadecimalt nummer,
> troligen för att skilja dublettmeddelanden (att sätta MQTT quality-of-service 2 
> är dock ett enklare sätt att undivika dubletter)

## BEACON - meddelanden
_då `<type>` = "BEACON"_

Beacon-meddelanden rapporterar status av widefind-fyrar
```
BEACON:<address>       ,<version>,<posX>,<posY>,<posZ>,<battery>,<rssi>,<timealive>,<calibration>,<nodetype>*<unknown>
BEACON:03FF5C0A2BFA3A9B,  0.2.7  , -10  ,  280 , 2500 ,   4.61  , 0.0  ,  764564   ,    MAN      ,   HUB    *  0EC7
```

> `<address>` är en 32-bittars address som unikt identifierar fyren.

> `<version>` är ett versionsnummer, troligtvis av det här protokollet.

> `<posX>`,`<posY>`,`<posZ>` är koordinaterna av fyren, mätta i centimeter.
> Med tanke på att fyrarna är fastskruvade i väggen så ändras nog inte det här nummret så ofta. 

> `<battery>` är batterispänning i volt. (troligtvis driftspänning för fyr)

> `<rssi>` (Received Signal Strength Indicator) är signalstyrka i decibel.
> [behöver mer info]

> `<timealive>` är upptid i milisekunder.

> `<calibration>` är kalibationsmetod. Värden `NONE` och `MAN` har setts.

> `<nodetype>` är antingen `HUB` eller `SAT`. En specifik fyr verkar alltid vara destignerad `HUB`
> med resten som `SAT`.

## REPORT - meddelanden
_då `<type>` = "REPORT"_

Rapporter på statusen av taggar

```
REPORT:   <address>     , <version>, <posX>, <posY> ,<posZ>, <velX>, <velY>, <velZ>, <battery>, <rssi>, <timealive> * <unknown>
REPORT:F1587D88122BE247 ,   0.2.7  , 4500  ,  -396  , 385  ,   0   ,   0   ,   0   ,   4.09   , -87.09,   1751321   *   D63D
```

> `<address>` är en 32-bittars address som unikt identifierar taggen.

> `<version>` är ett versionsnummer, troligtvis av det här protokollet.

> `<posX>`,`<posY>`,`<posZ>` är koordinaterna av taggen, mätta i centimeter. 

> `<velX>`,`<velY>`,`<velZ>` är hastigheten av taggen. 

> `<battery>` är batterispänning i volt.

> `<rssi>` (Received Signal Strength Indicator) är signalstyrka i decibel.
> [behöver mer info]

> `<timealive>` är upptid i milisekunder.


## RANGE - meddelanden
_då `<type>` = "RANGE" eller `<type>` = "SRANGE"_

Okänd betydelse, har aldrig stötts på. Troligtvis kräver någon form av förfrågan för att ske.

```
RANGE:<distance>,<address1>,<address2>,<rssi>*<unknown>
SRANGE:<distance>,<address1>,<address2>,<rssi>*<unknown>
```

## DIST - meddelanden
_då `<type>` = "DIST"_

Okänd betydelse, har aldrig stötts på. Troligtvis kräver någon form av förfrågan för att ske.
Troligtvis avståndet mellan två valda enheter.

```
DIST:<distance>,<address1>,<address2>,<rssi>,<success>*<unknown>
```
> `<rssi>` (Received Signal Strength Indicator) är signalstyrka i decibel.
> [behöver mer info]

> `<success>` är 0 om "misslyckad", och mer än 0 om "lyckad"
