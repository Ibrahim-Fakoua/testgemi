# Projet-Integration
## Objectifs
Liste de features à inclure dans le projet d'ici la 'final release' (connais pas la bonne expression en francais). Faque une checklist en gros.


## Progrès
Status de la progression vers l'objectif final

## Modèle
dans quoi on le fait, python, java ou autre

## L'équipe
L'équipe est composé de Eloi Blaser, Alex, Robitaille, Médérik Comeau-Trudel et d'Ibrahim Fakoua.

## Code convention :
Les standards qu'on se sert pour le développement du projet.

#### Coté Script
Les classes sont en **PascalCase**.
Les variables sont en **camelCase**
Les methodes sont en **snake_case**

#### Coté Godot
WIP

#### Définition Termes
Tile: Unité de l'environment de simulation le plus petit
Chunk: Partie d'environement composé de Tiles. Grandeur fixe.
Region: Groupe de chunks liés
Biome: Partie d'environment composé de régions liées
World: Le monde entier

Entity: Entité existant dans la simulation étant affiché et ayant une position. A la capacité de scheduler des événements
TileEntity: Entité ayant un behavior fixe prédéfini. Ne bouge pas de Tile.
Creature: Entité ayant un behavior composé de States liées entres elles par des Conditions. Peut mourir, se reproduire, se nourrir, et avoir une progéniture qui a un behavior qui évolue
State: État possèdant une liste de conditions d'entrée et de conditions de sortie, et une Action loop composée d'une liste d'actions que la créature effectue jusqu'à ce qu'elle change de State
Condition: Les différents triggers qui peuvent lier les states des créatures entre elles. Remplir une condition est ce qui fait changer la State d'une Creature
Action: Action de base effectuée par une créature selon la state dans laquelle elle est

Scheduler: Classe qui s'occupe du passage de temps dans laquelle les entités ''schedule'' des événements à des moments choisis.

## Versioning
Je (Eloi) reccommend qu'on se sert de SemVer pour les releases.
Extrait de `Semver.org`:

    Given a version number MAJOR.MINOR.PATCH, increment the:

    MAJOR version when you make incompatible API changes
    MINOR version when you add functionality in a backward compatible manner
    PATCH version when you make backward compatible bug fixes

    Additional labels for pre-release and build metadata are available as
    extensions to the MAJOR.MINOR.PATCH format.
