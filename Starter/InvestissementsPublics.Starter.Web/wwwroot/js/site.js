// =============================================================================
// SIIP — comportements globaux (chargé sur toutes les pages du _Layout)
// Dépendances : jQuery 3.6, Bootstrap 4.6, DataTables 1.10 (facultatif)
// =============================================================================
(function ($) {
    "use strict";

    // -------------------------------------------------------------------------
    // 1. Mémoriser l'état réduit/étendu de la sidebar entre les pages
    // -------------------------------------------------------------------------
    var SIDEBAR_KEY = "siip.sidebar.toggled";
    function storageGet(k) { try { return window.localStorage.getItem(k); } catch (e) { return null; } }
    function storageSet(k, v) { try { window.localStorage.setItem(k, v); } catch (e) { /* ignore */ } }

    if (storageGet(SIDEBAR_KEY) === "1" && $(window).width() >= 768) {
        $("body").addClass("sidebar-toggled");
        $(".sidebar").addClass("toggled");
    }
    $("#sidebarToggle, #sidebarToggleTop").on("click", function () {
        // sb-admin-2.js a déjà basculé la classe : on enregistre le nouvel état
        storageSet(SIDEBAR_KEY, $(".sidebar").hasClass("toggled") ? "1" : "0");
    });

    // -------------------------------------------------------------------------
    // 2. DataTables : toute <table class="datatable"> est enrichie automatiquement
    //    Options par attribut : data-page-length="25", data-order='[[1,"desc"]]'
    // -------------------------------------------------------------------------
    var DT_FR = {
        processing: "Traitement en cours…",
        search: "Rechercher :",
        lengthMenu: "Afficher _MENU_ éléments",
        info: "Affichage de _START_ à _END_ sur _TOTAL_ éléments",
        infoEmpty: "Aucun élément à afficher",
        infoFiltered: "(filtré de _MAX_ éléments au total)",
        loadingRecords: "Chargement…",
        zeroRecords: "Aucun élément correspondant trouvé",
        emptyTable: "Aucune donnée disponible",
        paginate: { first: "Premier", previous: "Précédent", next: "Suivant", last: "Dernier" },
        aria: { sortAscending: ": tri croissant", sortDescending: ": tri décroissant" }
    };

    if ($.fn.DataTable) {
        $.extend(true, $.fn.dataTable.defaults, { language: DT_FR, pageLength: 10, autoWidth: false });
        $("table.datatable").each(function () { $(this).DataTable(); });
    }

    // -------------------------------------------------------------------------
    // 3. Confirmation avant action destructive
    //    <form data-confirm="Supprimer cet utilisateur ?"> ou <a data-confirm="…">
    // -------------------------------------------------------------------------
    $(document).on("submit", "form[data-confirm]", function (e) {
        if (!window.confirm($(this).data("confirm"))) { e.preventDefault(); }
    });
    $(document).on("click", "a[data-confirm], button[data-confirm]:not([type=submit])", function (e) {
        if (!window.confirm($(this).data("confirm"))) { e.preventDefault(); e.stopImmediatePropagation(); }
    });

    // -------------------------------------------------------------------------
    // 4. Fermeture automatique des messages de succès après 5 s
    // -------------------------------------------------------------------------
    setTimeout(function () { $(".alert[data-autohide=true]").alert("close"); }, 5000);

    // -------------------------------------------------------------------------
    // 5. Infobulles et popovers Bootstrap
    // -------------------------------------------------------------------------
    $('[data-toggle="tooltip"]').tooltip();
    $('[data-toggle="popover"]').popover();

    // -------------------------------------------------------------------------
    // 6. Empêcher le double envoi des formulaires
    // -------------------------------------------------------------------------
    $(document).on("submit", "form:not([data-allow-resubmit])", function (e) {
        if (e.isDefaultPrevented()) { return; }
        var $btn = $(this).find("[type=submit]");
        setTimeout(function () { $btn.prop("disabled", true); }, 0);
    });

    // -------------------------------------------------------------------------
    // 7. Utilitaires exposés aux pages : SIIP.formatMontant(1234567) -> "1 234 567 HTG"
    // -------------------------------------------------------------------------
    window.SIIP = window.SIIP || {};
    window.SIIP.formatMontant = function (value, devise) {
        var n = Number(value) || 0;
        return n.toLocaleString("fr-FR", { maximumFractionDigits: 0 }) + " " + (devise || "HTG");
    };
    window.SIIP.colors = {
        primary: "#1f4e8c", success: "#1cc88a", info: "#36b9cc",
        warning: "#f6c23e", danger: "#e74a3b", secondary: "#858796", accent: "#d4a017"
    };

})(jQuery);
