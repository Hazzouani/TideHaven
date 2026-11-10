FISH FLOP ANIMATIES
===================

In deze map: 34 vissen (FBX). Elke vis heeft een rig en 2 animaties:
  Flop_Floor  (2,4 sec loop) vis ligt op zijn zij, spartelt, springt en draait om
  Flop_Rod    (2,0 sec loop) vis hangt met de kop omhoog aan de haak en spartelt

Scripts:
  Assets/scripts/FishFlop.cs                 speelt de animaties af
  Assets/scripts/Editor/FishFlopImporter.cs  zet de import-instellingen goed
                                             (Rig = Generic, Loop Time aan)
  Het importer-script werkt alleen voor FBX-bestanden in een map met "FishFlop"
  in de naam, dus hernoem deze map niet.

Zo gebruik je het
1. Sleep een vis-FBX in je scene.
2. Voeg het FishFlop component toe. Unity voegt ook een Animator toe (Controller leeg laten).
3. Klik op het pijltje van de FBX en sleep Flop_Floor naar "Floor Clip"
   en Flop_Rod naar "Rod Clip".
4. In je eigen code:
      fish.GetComponent<FishFlop>().OnFloor();      // vis op de vloer
      fish.GetComponent<FishFlop>().OnRod();        // vis aan de hengel
      fish.GetComponent<FishFlop>().StopFlopping(); // terug naar normale houding

Draaipunt
- Flop_Rod:   draaipunt = de bek. Maak de vis een child van de haak, local position 0,0,0.
- Flop_Floor: draaipunt = midden van de vis op de vloer. Zet de vis op de grond.

Tips
- Alle vissen hebben dezelfde botnamen, dus clips werken ook op andere vissen.
- "Speed" maakt het spartelen sneller of langzamer.
- "Random Start" zorgt dat een groep vissen niet tegelijk beweegt.
