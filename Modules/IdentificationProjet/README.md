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
| `DIP` | Examine toutes les fiches au statut Transmise à la DIP |
| `Admin` | Tout |

Les rôles `UEP` et `DIP` sont créés par `IdentitySeeder` ; il faut les attribuer aux utilisateurs concernés.

## Base de données Oracle (types et tables objet)

Scripts dans `Database/`, à exécuter dans le schéma applicatif (SQL*Plus / SQLcl) :

```
SQL> @00_installer.sql      -- 01_types → 02_tables → 03_types_corps → 04_vues → 05_package
SQL> @99_desinstaller.sql   -- suppression complète (données comprises)
```

| Objet | Rôle |
|---|---|
| `T_IP_CONTACT` | Type objet imbriqué (1.1 Contact principal) |
| `T_IP_FICHE` / `TB_IP_FICHE` | Fiche + méthodes `etape_complete`, `progression`, `est_modifiable` |
| `T_IP_IDEE_POSSIBLE` / `TB_IP_IDEE_POSSIBLE` | Idées possibles (3.1 à 3.5) ; index unique : une seule meilleure idée par fiche |
| `T_IP_ANNEXE` / `TB_IP_ANNEXE` | Documents annexes (BLOB SecureFile) |
| `T_IP_HISTORIQUE` / `TB_IP_HISTORIQUE` | Historique des transitions |
| `V_IP_*` | Vues relationnelles lues par EF Core (les méthodes des types y sont exposées en colonnes) |
| `PKG_IP_IDENTIFICATION` | **Seul point d'écriture** : création, mise à jour, idées, meilleure idée, annexes, transitions. Erreurs métier `ORA-20001` à `ORA-20010`, affichées telles quelles à l'utilisateur |

Les scripts ont été testés sur Oracle Database 23ai Free. Ils n'utilisent que des fonctionnalités disponibles depuis 12c.

## Code

```
IdentificationProjet.Application     Domaine (entités, statuts, actions), règles d'accès, contrat du dépôt
IdentificationProjet.Infrastructure  DbContext EF Core sur les vues (lecture) + appels ODP.NET au package (écriture)
IdentificationProjet.Web             Bibliothèque Razor : Area « IdentificationProjet » (FichesController + vues)
```

Le module est branché dans le Starter (`Program.cs` : `AddApplicationPart` + `AddIdentificationProjet(...)`)
et apparaît dans le menu latéral sous « Identification projet ».
