🎮 GAME DESIGN DOCUMENT — Handler Protocol

🧩 1. Pitch
Handler Protocol est un jeu coopératif multijoueur asymétrique en vue FPS.
 L’objectif : escorter un artefact jusqu’à une zone d’exfiltration tout en survivant à des vagues d’ennemis de plus en plus puissants.
L’équipe est composée de deux types de joueurs :
Les Ciphers, soldats d’élite sur le terrain.


Le Handler, entité tactique qui les supervise et les soutient via un drone de commandement.


Ensemble, ils doivent coordonner leurs forces pour accomplir leur mission, entre combat, stratégie et gestion des ressources.

⚙️ 2. Concept clé
Le jeu repose sur une coopération asymétrique :
Les Ciphers agissent directement sur le terrain, combattent et transportent l’objectif.


Le Handler observe, analyse et influence la bataille depuis une vue FPS de son drone, capable d’interagir en temps réel.


Le cœur du gameplay : communication + tension + synergie des rôles.

🎯 3. Objectif principal
Escorter un artefact énergétique instable jusqu’à son point d’exfiltration.
Les ennemis cherchent à détruire le porteur ou à s’emparer de l’artefact.
 L’équipe doit :
Protéger le porteur.


Gérer ses ressources et munitions.


Survivre aux vagues d’ennemis adaptatifs.


Remplir des objectifs secondaires pour activer le soutien du Handler.



🧠 4. Rôles jouables

⚔️ Les Ciphers
Des soldats augmentés, synchronisés à leur Handler.
 Chaque Cipher possède un style de jeu distinct, inspiré de rôles de combat complémentaires.
Vue :
FPS immersive


Combats nerveux et lisibles


Accent sur la coordination et la complémentarité


Exemple de classes :
Nom
Rôle
Style de jeu
Capacités
Surge
Assaut
Frontline rapide et offensif
Dash électrifié, grenade à plasma, boost d’adrénaline
Aegis
Défenseur
Tank / contrôle de zone
Bouclier énergétique, mur déployable, provocation
Patch
Support
Soin et utilitaire
Drone médical, zone de soin, transfert d’énergie
Echo
Recon
Vision / infiltration
Camouflage optique, capteur de mouvement, marqueur de cibles

Chaque Cipher possède un kit modulaire et un style visuel unique (armure, visière, effets de lumière correspondant à son rôle).

🧭 Le Handler
Un joueur clé, incarnant une intelligence tactique consciente, connectée au champ de bataille via un drone d’observation à la première personne.
Vue :
FPS depuis le drone tactique


Mobilité aérienne limitée (hauteur et portée)


Vision augmentée (détection, balayage thermique, signal audio)


Rôle :
Surveille et soutient les Ciphers.


Active des capacités de soutien via un système de ressources.


Interagit avec l’environnement (hacking, scans, leurres, déploiements).


Participe activement grâce à des mini-jeux de piratage et de gestion réseau.



🧬 5. Archétypes de Handlers
Nom
Spécialité
Style
Capacités principales
ARCHON
Frappe / Contrôle
Militaire, autoritaire
Frappe orbitale, brouillage d’armes, buff de dégâts
FABER
Logistique / Défense
Ingénieur, protecteur
Tourelles, drones de réparation, colis de ravitaillement
SYNTHEX
Analyse / Anticipation
Observateur, analytique
Scan neural, prédiction de trajectoire, boost de cooldown
VOID
Sabotage / Distorsion
Mystique, instable
Contrôle mental, champ de ralentissement, brouillage total

Chaque Handler dispose d’un drone spécifique visuellement identifiable et d’un style d’interface holographique.
 Les capacités s’activent via une énergie de protocole, rechargée grâce aux actions réussies des Ciphers.

💡 6. Gameplay Asymétrique Coopératif
Boucle de jeu :
Déploiement → Le Handler choisit le point d’entrée de l’équipe.


Escorte → Les Ciphers progressent et affrontent les vagues ennemies.


Soutien → Le Handler apporte des boosts, soins, infos, ou frappes ciblées.


Menace adaptative → L’ennemi s’ajuste (plus rapide, plus nombreux, capacités spéciales).


Extraction finale → Défense de la zone jusqu’à l’arrivée du transport.


Objectifs secondaires :
Points de piratage, relais de communication, balises de données.


Permettent d’activer de nouveaux pouvoirs pour le Handler ou d’améliorer l’équipement des Ciphers.



🔄 7. Progression
XP par classe → Chaque classe de Cipher et chaque Handler progresse indépendamment.


Déblocage de nouveaux équipements et d’autres handler


Système de personnalisation visuelle (armures, skins de drone).



🧱 8. Structure technique (Unity)
Système
Description
Networking
Framework : Netcode for GameObjects (Unity) ou Photon Fusion selon test perf
Gameplay
FPS Controller (Ciphers) + Drone Controller (Handler)
AI System
Enemies avec comportement adaptatif (navmesh + comportement modulaire)
UI
HUD holographique, minimap tactique, système de ping
Map Design
Corridors + zones ouvertes à défendre (inspiration : Extraction zones, payloads)


🎨 9. Direction artistique
Univers : Futuriste / techno-militaire.


Ambiance : mélange de clean tech (interfaces holographiques, surfaces lisses, lumière blanche) et de chaos industriel (zones de combat, ruines technologiques).


Palette : bleus froids, violets, oranges électriques.


Inspiration : Evolve, The Division, Deus Ex.



🔊 10. Direction sonore
Voix synthétiques, échos de réseau, effets de données corrompues.


Thèmes musicaux dynamiques selon l’intensité du combat.


Communications tactiques (“Handler online”, “Cipher down”, etc.).



🌐 11. Identité narrative
Dans un futur où la conscience humaine est intégrée à des réseaux militaires,
 les Handlers sont les esprits augmentés d’anciens stratèges,
 chargés de diriger les Ciphers, soldats synchronisés à leur fréquence.
Ensemble, ils exécutent le Protocole — la dernière ligne de défense d’une humanité au bord de l’extinction.

🧩 12. Expérience Joueur
Type de joueur
Ce qu’il recherche
Ce que le jeu lui offre
Joueur d’action
Adrénaline, combat fluide
FPS nerveux, progression claire
Joueur stratégique
Contrôle, planification
Vue tactique, capacités de soutien
Joueur coopératif
Communication, synergie
Rôles complémentaires, actions combinées
Joueur créatif
Personnalisation
Skins, builds de classes, modules de drone


🚀 13. Résumé
Handler Protocol combine la tension du FPS coopératif à l’intelligence d’un jeu tactique asymétrique.
 Les joueurs doivent fusionner leurs forces, équilibrer le terrain et gérer la menace ennemie — ensemble.
Chaque mission devient un champ de bataille où la coordination est la clé de la survie.