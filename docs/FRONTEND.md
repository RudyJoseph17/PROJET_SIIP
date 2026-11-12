# Front-end SIIP — thème SB Admin 2

Le front-end est basé sur [SB Admin 2](https://github.com/startbootstrap/startbootstrap-sb-admin-2) v4.1.4 (licence MIT).
Il est **centralisé dans le Starter** : les modules (Areas) n'embarquent pas leur propre layout ni leurs propres librairies.

## Organisation

```
Starter/InvestissementsPublics.Starter.Web/
├── Views/Shared/
│   ├── _Layout.cshtml        layout principal : sidebar + topbar + footer (pages connectées)
│   ├── _LayoutAuth.cshtml    layout des pages publiques (connexion…), sans sidebar
│   ├── _Sidebar.cshtml       menu latéral (liste des modules, section Administration)
│   ├── _Topbar.cshtml        barre du haut : recherche, menu utilisateur
│   ├── _Alerts.cshtml        messages flash TempData["Success"|"Error"|"Warning"|"Info"]
│   └── _LogoutModal.cshtml   confirmation de déconnexion (POST /Home/Logout)
└── wwwroot/
    ├── vendor/               librairies tierces — NE PAS MODIFIER
    │   ├── sb-admin-2/       CSS + JS du thème (Bootstrap 4.6 inclus dans le CSS)
    │   ├── bootstrap4/       bootstrap.bundle.min.js (4.6, avec Popper)
    │   ├── fontawesome-free/ icônes Font Awesome 5 (+ webfonts)
    │   ├── jquery-easing/    requis par sb-admin-2.js
    │   ├── datatables/       tableaux triables / paginés
    │   └── chart.js/         graphiques (v2.9, chargé à la demande par les pages)
    ├── lib/jquery, lib/jquery-validation*   (déjà présents, réutilisés)
    ├── css/site.css          surcharges du projet (couleurs, composants, compat BS5)
    └── js/site.js            comportements globaux
```

## Brancher un module sur le layout commun

Dans `Areas/<Module>/Views/_ViewStart.cshtml` du module :

```cshtml
@{
    Layout = "/Views/Shared/_Layout.cshtml";
}
```

Puis enregistrer le module dans `Program.cs` du Starter (`AddApplicationPart(...)`) et ajouter son `ProjectReference`.
Le menu latéral détecte automatiquement les Areas chargées : un module non encore branché s'affiche grisé.
Pour ajouter une entrée au menu, compléter le tableau `modules` dans `_Sidebar.cshtml`.

## Écrire une page

```cshtml
@{
    ViewData["Title"] = "Liste des projets";          // titre onglet + en-tête de page
    ViewData["Subtitle"] = "Banque de projets";        // facultatif
}

@section PageActions {                                  @* boutons à droite du titre *@
    <a asp-action="Create" class="btn btn-sm btn-primary shadow-sm"><i class="fas fa-plus fa-sm mr-1"></i> Nouveau</a>
}

<div class="card shadow mb-4">
    <div class="card-header py-3">Projets</div>
    <div class="card-body">
        <table class="table table-bordered datatable" data-page-length="25"> ... </table>
    </div>
</div>

@section Scripts {
    <script src="~/vendor/chart.js/Chart.min.js"></script>   @* uniquement si la page a des graphiques *@
}
```

Conventions fournies par `site.js` :

| Attribut / classe                   | Effet                                                       |
|-------------------------------------|-------------------------------------------------------------|
| `<table class="datatable">`         | DataTables en français (tri, recherche, pagination)         |
| `data-page-length`, `data-order`    | options DataTables par attribut                             |
| `<form data-confirm="Supprimer ?">` | demande de confirmation avant envoi                         |
| `data-toggle="tooltip"`             | infobulle Bootstrap                                         |
| `SIIP.formatMontant(n, "HTG")`      | formatage `1 234 567 HTG`                                   |
| `SIIP.colors`                       | palette pour Chart.js                                       |

Les formulaires sont protégés contre le double envoi (ajouter `data-allow-resubmit` pour désactiver).

## Bootstrap 4 vs 5

SB Admin 2 utilise **Bootstrap 4.6** : utiliser `data-toggle` / `data-target` / `data-dismiss`,
`mr-*` / `ml-*`, `text-right`, `custom-select`, `form-group`.
Le gabarit ASP.NET d'origine (Bootstrap 5, `wwwroot/lib/bootstrap`) n'est plus chargé par le layout.
Quelques classes Bootstrap 5 déjà utilisées (`g-2`, `text-end`, `form-select`, `gap-2`…) sont émulées dans
`site.css` (section 6) en attendant la migration des vues concernées.

## Personnaliser les couleurs

Modifier les variables en tête de `wwwroot/css/site.css` (`--siip-primary`, `--siip-accent`…).
Ne jamais modifier les fichiers sous `wwwroot/vendor/`.

## Police

La police Nunito est chargée depuis Google Fonts. Pour un déploiement sans accès Internet (intranet),
télécharger les fichiers de police dans `wwwroot/vendor/fonts/` et remplacer la balise `<link>` dans les deux layouts ;
à défaut, le navigateur se replie sur la police système.
