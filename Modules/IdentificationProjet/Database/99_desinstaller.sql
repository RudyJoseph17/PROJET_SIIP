-- =============================================================================
-- SIIP - Module "Identification projet" : désinstallation (SUPPRIME LES DONNÉES)
-- Les erreurs « objet inexistant » sont ignorées (installation partielle ou v1).
-- =============================================================================
SET DEFINE OFF
DECLARE
    PROCEDURE executer (p_sql IN VARCHAR2) IS
    BEGIN
        EXECUTE IMMEDIATE p_sql;
    EXCEPTION
        WHEN OTHERS THEN NULL;   -- objet déjà absent
    END;
BEGIN
    executer('DROP PACKAGE pkg_ip_etude');
    executer('DROP PACKAGE pkg_ip_identification');
    FOR v IN (SELECT view_name FROM user_views WHERE view_name LIKE 'V\_IP\_%' ESCAPE '\' OR view_name = 'V_BP_PROJET_VALIDE') LOOP
        executer('DROP VIEW ' || v.view_name);
    END LOOP;
    -- Tables : les dépendantes d'abord
    executer('DROP TABLE tb_ip_avis PURGE');
    executer('DROP TABLE tb_ip_etude_document PURGE');
    executer('DROP TABLE tb_ip_etude PURGE');
    executer('DROP TABLE tb_ip_grille PURGE');
    executer('DROP TABLE tb_ip_historique PURGE');
    executer('DROP TABLE tb_ip_annexe PURGE');
    executer('DROP TABLE tb_ip_idee_possible PURGE');
    executer('DROP TABLE tb_ip_fiche PURGE');
    executer('DROP TABLE tb_ip_question PURGE');
    executer('DROP TABLE tb_ip_lieu PURGE');
    FOR s IN (SELECT sequence_name FROM user_sequences WHERE sequence_name LIKE 'SEQ\_IP\_%' ESCAPE '\') LOOP
        executer('DROP SEQUENCE ' || s.sequence_name);
    END LOOP;
    -- Types : les composites avant leurs composants
    executer('DROP TYPE t_ip_avis FORCE');
    executer('DROP TYPE t_ip_etude_document FORCE');
    executer('DROP TYPE t_ip_etude FORCE');
    executer('DROP TYPE t_ip_grille FORCE');
    executer('DROP TYPE t_ip_reponse_tab FORCE');
    executer('DROP TYPE t_ip_reponse FORCE');
    executer('DROP TYPE t_ip_piste_tab FORCE');
    executer('DROP TYPE t_ip_piste FORCE');
    executer('DROP TYPE t_ip_question FORCE');
    executer('DROP TYPE t_ip_historique FORCE');
    executer('DROP TYPE t_ip_annexe FORCE');
    executer('DROP TYPE t_ip_idee_possible FORCE');
    executer('DROP TYPE t_ip_fiche FORCE');
    executer('DROP TYPE t_ip_localisation FORCE');
    executer('DROP TYPE t_ip_contact FORCE');
    executer('DROP TYPE t_ip_lieu FORCE');
END;
/
PROMPT Module Identification projet désinstallé.
