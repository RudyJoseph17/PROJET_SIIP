-- =============================================================================
-- SIIP - Module "Identification projet" : désinstallation (SUPPRIME LES DONNÉES)
-- =============================================================================
SET DEFINE OFF
DROP PACKAGE pkg_ip_identification;
DROP VIEW v_ip_historique;
DROP VIEW v_ip_annexe;
DROP VIEW v_ip_idee_possible;
DROP VIEW v_ip_fiche;
DROP TABLE tb_ip_historique PURGE;
DROP TABLE tb_ip_annexe PURGE;
DROP TABLE tb_ip_idee_possible PURGE;
DROP TABLE tb_ip_fiche PURGE;
DROP SEQUENCE seq_ip_historique;
DROP SEQUENCE seq_ip_annexe;
DROP SEQUENCE seq_ip_idee;
DROP SEQUENCE seq_ip_fiche;
DROP TYPE t_ip_historique FORCE;
DROP TYPE t_ip_annexe FORCE;
DROP TYPE t_ip_idee_possible FORCE;
DROP TYPE t_ip_fiche FORCE;
DROP TYPE t_ip_contact FORCE;
