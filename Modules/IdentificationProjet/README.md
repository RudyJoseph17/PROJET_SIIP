# Module « Identification projet »

Saisie et suivi de la **Fiche d'Identification de l'Idée de Projet**
(Manuel de procédures, Tome I, §3 et Annexe 3), avec le circuit d'examen promoteur → UEP → DIP (§3.E).

## Adaptations par rapport à la fiche du manuel

| Manuel | Module |
|---|---|
| 1.2 Titre proposé, dans l'introduction | Saisi **après** la détermination de la meilleure idée (3.5) ; masqué tant qu'aucune idée n'est cochée, et effacé en base si le choix est retiré |
| 3.1 à 3.4, sections séparées | **Un tableau** : une ligne par idée (nombre illimité), colonnes Idée possible / Légitimité / Pertinence / Viabilité |
| 3.5 Meilleure idée, texte libre | Chaque idée du tableau reprise avec une **case à cocher** (une seule possible, contrainte d'unicité en base) + justification |
| Pièces jointes « s'il y a lieu » | **Section 5 : documents annexes**, 3 au maximum, 10 Mo chacun (pdf, Word, Excel, PowerPoint, jpg, png) |

## Version 2 : grille d'analyse du MPCE

À **chaque arrivée** d'une fiche au MPCE (statut « Soumise au MPCE »), la base génère automatiquement une
**grille d'analyse** conforme au modèle *GRILLE D'ANALYSE - IDEE PROJET*. Une nouvelle grille est créée à
chaque soumission : les grilles précédentes sont conservées.

| Grille | Source |
|---|---|
| Ministère / Institution | 1.1 Nom de l'organisation |
| Titre de l'idée de projet | Intitulé de la meilleure idée (3.5) |
| Soumis par l'UEP le / Reçu par la DIP le | Historique (approbation UEP / arrivée au MPCE) |
| 1- Localisation | Nouvelle section 1.4 de la fiche (référentiel : 10 départements, 42 arrondissements, 145 communes, 571 sections communales) |
| 2- Problème · 3- Objectif général · Données quantitatives / qualitatives | 1.3 et 2.1 (séparés dans la fiche) |
| 5- Pistes de solutions · 6- Meilleure idée | Copie des idées (3.1) et de l'idée retenue (3.5) |
| 7- Pertinence de l'idée choisie | Pertinence, légitimité et viabilité de la meilleure idée |
| Généralités (11 questions) | Oui / Non, **commentaire obligatoire si Non** |
| Résultats de l'analyse | Accepté → fiche **Validée** · Doit être révisé → fiche **À corriger** · Rejeté → fiche **Rejetée** |

La grille contient une **copie** de la fiche (tables imbriquées Oracle) : elle reste conforme à la version analysée,
même si la fiche est corrigée ensuite.

**Circuit de la grille** : Analyste de projet (`DIP_ANALYSTE`) remplit et transmet → Chef de Service
(`DIP_CHEF_SERVICE`) valide ou renvoie → Directeur (`DIP_DIRECTEUR`, « Vu et approuvé ») approuve ou renvoie.
Le résultat ne s'applique à la fiche qu'à l'approbation du Directeur. Une même personne ne peut pas valider une grille
qu'elle a rédigée ou validée (sauf Admin). Les renvois internes ne sont pas visibles du promoteur.

**Modification de la fiche** : impossible dès la soumission au MPCE ; de nouveau possible si le résultat est
« À corriger » (la resoumission retourne directement au MPCE et génère une nouvelle grille) ; définitivement
impossible une fois la fiche validée.

**Export** : bouton *Excel* (modèle officiel rempli, colonne « COMMENTAIRES » ajoutée aux Généralités) et *Imprimer*.

## Suivi de la progression

- **Rédaction** : 5 étapes obligatoires (Introduction · Importance · Idées possibles · Meilleure idée + titre · Conclusion), soit 0 à 100 %.
  Les règles sont codées une seule fois, dans les méthodes du type objet `T_IP_FICHE` (`etape_complete`, `progression`).
  Les annexes sont facultatives. La soumission exige 100 %.
- **Circuit** :

```
BROUILLON ──soumettre──▶ SOUMISE_UEP ──approuver──▶ TRANSMISE_DIP ──approuver──▶ APPROUVEE_DIP (Avant-Projet)
                          │  ├─complément─▶ COMPLEMENT_UEP ─soumettre─▶ SOUMISE_UEP
                          │  └─rejeter────▶ REJETEE_UEP
                          TRANSMISE_DIP ├─complément─▶ COMPLEMENT_DIP ─soumettre─▶ TRANSMISE_DIP
                                        └─rejeter────▶ REJETEE_DIP
```

  Un commentaire est obligatoire pour un complément ou un rejet. Chaque transition est historisée.

## Rôles

| Rôle | Droits |
|---|---|
| Tout utilisateur connecté (promoteur) | Crée ses fiches, les modifie en Brouillon / Complément demandé, les soumet |
| `UEP` | Examine les fiches **de son ministère** (`AspNetUsers.Ministere` = ministère du promoteur) au statut Soumise à l'UEP |
| `DIP` | Consulte toutes les fiches et grilles |
| `DIP_ANALYSTE` / `DIP_CHEF_SERVICE` / `DIP_DIRECTEUR` | Circuit de la grille d'analyse (voir plus haut) |
| `Admin` | Tout |

Les rôles `UEP`, `DIP`, `DIP_ANALYSTE`, `DIP_CHEF_SERVICE` et `DIP_DIRECTEUR` sont créés par `IdentitySeeder` ; il faut les attribuer aux utilisateurs concernés.

## Base de données Oracle (types et tables objet)

Scripts dans `Database/`, à exécuter dans le schéma applicatif (SQL*Plus / SQLcl) :

```
SQL> @00_installer.sql          -- base neuve : scripts 01 à 13 + contrôle des objets invalides
SQL> @00_mise_a_jour_v2.sql     -- base déjà en v1 : scripts 06 à 13 (données conservées)
SQL> @99_desinstaller.sql       -- suppression complète (données comprises)
```

| Objet | Rôle |
|---|---|
| `T_IP_CONTACT` | Type objet imbriqué (1.1 Contact principal) |
| `T_IP_FICHE` / `TB_IP_FICHE` | Fiche + méthodes `etape_complete`, `progression`, `est_modifiable` |
| `T_IP_IDEE_POSSIBLE` / `TB_IP_IDEE_POSSIBLE` | Idées possibles (3.1 à 3.5) ; index unique : une seule meilleure idée par fiche |
| `T_IP_ANNEXE` / `TB_IP_ANNEXE` | Documents annexes (BLOB SecureFile) |
| `T_IP_HISTORIQUE` / `TB_IP_HISTORIQUE` | Historique des transitions |
| `V_IP_*` | Vues relationnelles lues par EF Core (les méthodes des types y sont exposées en colonnes) |
| `T_IP_LOCALISATION`, `T_IP_LIEU` / `TB_IP_LIEU` | Localisation de la fiche et référentiel géographique |
| `T_IP_GRILLE` / `TB_IP_GRILLE` | Grille d'analyse ; tables imbriquées `PISTES` (`T_IP_PISTE_TAB`) et `REPONSES` (`T_IP_REPONSE_TAB`) ; méthodes `anomalie`, `nb_non` |
| `T_IP_QUESTION` / `TB_IP_QUESTION` | Questions « Généralités » (désactivables : `actif = 0`) |
| `PKG_IP_IDENTIFICATION` | **Seul point d'écriture** : création, mise à jour, idées, meilleure idée, annexes, transitions. Erreurs métier `ORA-20001` à `ORA-20014`, affichées telles quelles à l'utilisateur |

Les scripts ont été testés sur Oracle Database 23ai Free. Ils n'utilisent que des fonctionnalités disponibles depuis 12c.

## Code

```
IdentificationProjet.Application     Domaine (entités, statuts, actions), règles d'accès, contrat du dépôt
IdentificationProjet.Infrastructure  DbContext EF Core sur les vues (lecture) + appels ODP.NET au package (écriture)
IdentificationProjet.Web             Bibliothèque Razor : Area « IdentificationProjet » (FichesController + vues)
```

Le module est branché dans le Starter (`Program.cs` : `AddApplicationPart` + `AddIdentificationProjet(...)`)
et apparaît dans le menu latéral sous « Identification projet ».
