-- ADR-0003 adım 5 (#149) — var olan veritabanını conjoined kiracılığa geçirir.
--
-- ÜRETİMİ: Marten'ın kendi göç üreticisi (CreateMigrationAsync).
-- ELLE DÜZELTİLEN TEK YER: üretilen betik shared.mt_events üzerindeki
-- fkey_mt_events_stream_id_tenant_id kısıtını İKİ KEZ ekliyordu; ikinci ekleme
-- "42710: constraint already exists" ile patlıyor ve BÜTÜN göçü geri alıyordu.
-- Yinelenen blok silindi, kalan ekleme DROP IF EXISTS ile idempotent yapıldı.
-- Aynı kusur ApplyAllDatabaseChangesOnStartup() yolunu da öldürüyor (ölçüldü:
-- MartenSchemaException, API açılışta düşüyor) — bu yüzden göç açılışta değil,
-- burada, elle ve gözden geçirilebilir biçimde yapılıyor.
--
-- YIKICI DEĞİLDİR: yalnız ALTER TABLE ... ADD COLUMN / birincil anahtar değişimi.
-- Ölçüldü: DROP TABLE / DELETE / TRUNCATE yok. Yine de önce yedek alın.
--
-- Tek transaction'da çalıştırın:
--   psql -d mesnet --single-transaction -v ON_ERROR_STOP=1 -f 149-conjoined-kiracilik.sql
-- Ardından 149-kiraci-damgalama.sql çalıştırılmalıdır: bu betik satırları
-- '*DEFAULT*' kovasında bırakır ve o hâlde hiçbir okul kendi verisini göremez.

DO $$
BEGIN      BEGIN
        EXECUTE 'CREATE SCHEMA IF NOT EXISTS shared';
      EXCEPTION
        WHEN duplicate_schema THEN NULL;
        WHEN unique_violation THEN NULL;
      END;

      BEGIN
        EXECUTE 'CREATE SCHEMA IF NOT EXISTS institution';
      EXCEPTION
        WHEN duplicate_schema THEN NULL;
        WHEN unique_violation THEN NULL;
      END;

      BEGIN
        EXECUTE 'CREATE SCHEMA IF NOT EXISTS attendance';
      EXCEPTION
        WHEN duplicate_schema THEN NULL;
        WHEN unique_violation THEN NULL;
      END;

      BEGIN
        EXECUTE 'CREATE SCHEMA IF NOT EXISTS enrollment';
      EXCEPTION
        WHEN duplicate_schema THEN NULL;
        WHEN unique_violation THEN NULL;
      END;

      BEGIN
        EXECUTE 'CREATE SCHEMA IF NOT EXISTS contract';
      EXCEPTION
        WHEN duplicate_schema THEN NULL;
        WHEN unique_violation THEN NULL;
      END;

      BEGIN
        EXECUTE 'CREATE SCHEMA IF NOT EXISTS payment';
      EXCEPTION
        WHEN duplicate_schema THEN NULL;
        WHEN unique_violation THEN NULL;
      END;

      BEGIN
        EXECUTE 'CREATE SCHEMA IF NOT EXISTS coordination';
      EXCEPTION
        WHEN duplicate_schema THEN NULL;
        WHEN unique_violation THEN NULL;
      END;

      BEGIN
        EXECUTE 'CREATE SCHEMA IF NOT EXISTS business';
      EXCEPTION
        WHEN duplicate_schema THEN NULL;
        WHEN unique_violation THEN NULL;
      END;

      BEGIN
        EXECUTE 'CREATE SCHEMA IF NOT EXISTS reporting';
      EXCEPTION
        WHEN duplicate_schema THEN NULL;
        WHEN unique_violation THEN NULL;
      END;

      BEGIN
        EXECUTE 'CREATE SCHEMA IF NOT EXISTS internship';
      EXCEPTION
        WHEN duplicate_schema THEN NULL;
        WHEN unique_violation THEN NULL;
      END;

      BEGIN
        EXECUTE 'CREATE SCHEMA IF NOT EXISTS security';
      EXCEPTION
        WHEN duplicate_schema THEN NULL;
        WHEN unique_violation THEN NULL;
      END;

END
$$;


alter table institution.mt_doc_academicperiod add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table institution.mt_doc_academicperiod drop constraint pkey_mt_doc_academicperiod_id CASCADE;
alter table institution.mt_doc_academicperiod add CONSTRAINT pkey_mt_doc_academicperiod_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table attendance.mt_doc_academicperiodview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table attendance.mt_doc_academicperiodview drop constraint pkey_mt_doc_academicperiodview_id CASCADE;
alter table attendance.mt_doc_academicperiodview add CONSTRAINT pkey_mt_doc_academicperiodview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table enrollment.mt_doc_academicperiodview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table enrollment.mt_doc_academicperiodview drop constraint pkey_mt_doc_academicperiodview_id CASCADE;
alter table enrollment.mt_doc_academicperiodview add CONSTRAINT pkey_mt_doc_academicperiodview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table contract.mt_doc_academicperiodview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table contract.mt_doc_academicperiodview drop constraint pkey_mt_doc_academicperiodview_id CASCADE;
alter table contract.mt_doc_academicperiodview add CONSTRAINT pkey_mt_doc_academicperiodview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table payment.mt_doc_academicperiodview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table payment.mt_doc_academicperiodview drop constraint pkey_mt_doc_academicperiodview_id CASCADE;
alter table payment.mt_doc_academicperiodview add CONSTRAINT pkey_mt_doc_academicperiodview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_academicperiodview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_academicperiodview drop constraint pkey_mt_doc_academicperiodview_id CASCADE;
alter table coordination.mt_doc_academicperiodview add CONSTRAINT pkey_mt_doc_academicperiodview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table attendance.mt_doc_attendancerecord add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table attendance.mt_doc_attendancerecord drop constraint pkey_mt_doc_attendancerecord_id CASCADE;
alter table attendance.mt_doc_attendancerecord add CONSTRAINT pkey_mt_doc_attendancerecord_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table attendance.mt_doc_attendanceview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table attendance.mt_doc_attendanceview drop constraint pkey_mt_doc_attendanceview_id CASCADE;
alter table attendance.mt_doc_attendanceview add CONSTRAINT pkey_mt_doc_attendanceview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_branchstudentcountview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_branchstudentcountview drop constraint pkey_mt_doc_branchstudentcountview_id CASCADE;
alter table coordination.mt_doc_branchstudentcountview add CONSTRAINT pkey_mt_doc_branchstudentcountview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_branchworkloadconfig add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_branchworkloadconfig drop constraint pkey_mt_doc_branchworkloadconfig_id CASCADE;
alter table coordination.mt_doc_branchworkloadconfig add CONSTRAINT pkey_mt_doc_branchworkloadconfig_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_businesscoordinationview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_businesscoordinationview drop constraint pkey_mt_doc_businesscoordinationview_id CASCADE;
alter table coordination.mt_doc_businesscoordinationview add CONSTRAINT pkey_mt_doc_businesscoordinationview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_businessevaluation add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_businessevaluation drop constraint pkey_mt_doc_businessevaluation_id CASCADE;
alter table coordination.mt_doc_businessevaluation add CONSTRAINT pkey_mt_doc_businessevaluation_tenant_id_id PRIMARY KEY (tenant_id, id);
CREATE TABLE IF NOT EXISTS payment.mt_doc_classyearcontributionclaim (
    id                  uuid                        NOT NULL,
    data                jsonb                       NOT NULL,
    mt_last_modified    timestamp with time zone    NULL DEFAULT (transaction_timestamp()),
    mt_version          uuid                        NOT NULL DEFAULT (md5(random()::text || clock_timestamp()::text)::uuid),
    mt_dotnet_type      varchar                     NULL,
CONSTRAINT pkey_mt_doc_classyearcontributionclaim_id PRIMARY KEY (id)
);

CREATE INDEX mt_doc_classyearcontributionclaim_idx_student_id ON payment.mt_doc_classyearcontributionclaim USING btree ((CAST(data ->> 'studentId' as uuid)));
alter table payment.mt_doc_contractemploymentview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table payment.mt_doc_contractemploymentview drop constraint pkey_mt_doc_contractemploymentview_id CASCADE;
alter table payment.mt_doc_contractemploymentview add CONSTRAINT pkey_mt_doc_contractemploymentview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_coordinationconfig add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_coordinationconfig drop constraint pkey_mt_doc_coordinationconfig_id CASCADE;
alter table coordination.mt_doc_coordinationconfig add CONSTRAINT pkey_mt_doc_coordinationconfig_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_coordinationplacedstudentview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_coordinationplacedstudentview drop constraint pkey_mt_doc_coordinationplacedstudentview_id CASCADE;
alter table coordination.mt_doc_coordinationplacedstudentview add CONSTRAINT pkey_mt_doc_coordinationplacedstudentview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table reporting.mt_doc_generateddocument add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table reporting.mt_doc_generateddocument drop constraint pkey_mt_doc_generateddocument_id CASCADE;
alter table reporting.mt_doc_generateddocument add CONSTRAINT pkey_mt_doc_generateddocument_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_guidancevisit add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_guidancevisit drop constraint pkey_mt_doc_guidancevisit_id CASCADE;
alter table coordination.mt_doc_guidancevisit add CONSTRAINT pkey_mt_doc_guidancevisit_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table business.mt_doc_institutionbranchview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table business.mt_doc_institutionbranchview drop constraint pkey_mt_doc_institutionbranchview_id CASCADE;
alter table business.mt_doc_institutionbranchview add CONSTRAINT pkey_mt_doc_institutionbranchview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table contract.mt_doc_internshipcontract add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table contract.mt_doc_internshipcontract drop constraint pkey_mt_doc_internshipcontract_id CASCADE;
alter table contract.mt_doc_internshipcontract add CONSTRAINT pkey_mt_doc_internshipcontract_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table enrollment.mt_doc_internshipplacement add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table enrollment.mt_doc_internshipplacement drop constraint pkey_mt_doc_internshipplacement_id CASCADE;
alter table enrollment.mt_doc_internshipplacement add CONSTRAINT pkey_mt_doc_internshipplacement_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table attendance.mt_doc_internshipplacementview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table attendance.mt_doc_internshipplacementview drop constraint pkey_mt_doc_internshipplacementview_id CASCADE;
alter table attendance.mt_doc_internshipplacementview add CONSTRAINT pkey_mt_doc_internshipplacementview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table internship.mt_doc_internshipsaga add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table internship.mt_doc_internshipsaga drop constraint pkey_mt_doc_internshipsaga_id CASCADE;
alter table internship.mt_doc_internshipsaga add CONSTRAINT pkey_mt_doc_internshipsaga_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table internship.mt_doc_internshipsummary add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table internship.mt_doc_internshipsummary drop constraint pkey_mt_doc_internshipsummary_id CASCADE;
alter table internship.mt_doc_internshipsummary add CONSTRAINT pkey_mt_doc_internshipsummary_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_monthlyactivityreport add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_monthlyactivityreport drop constraint pkey_mt_doc_monthlyactivityreport_id CASCADE;
alter table coordination.mt_doc_monthlyactivityreport add CONSTRAINT pkey_mt_doc_monthlyactivityreport_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table attendance.mt_doc_paidleaverequest add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table attendance.mt_doc_paidleaverequest drop constraint pkey_mt_doc_paidleaverequest_id CASCADE;
alter table attendance.mt_doc_paidleaverequest add CONSTRAINT pkey_mt_doc_paidleaverequest_tenant_id_id PRIMARY KEY (tenant_id, id);
CREATE TABLE IF NOT EXISTS payment.mt_doc_paymentsaga (
    tenant_id           varchar                     NOT NULL DEFAULT '*DEFAULT*',
    id                  uuid                        NOT NULL,
    data                jsonb                       NOT NULL,
    mt_last_modified    timestamp with time zone    NULL DEFAULT (transaction_timestamp()),
    mt_dotnet_type      varchar                     NULL,
    mt_version          bigint                      NOT NULL DEFAULT 0,
CONSTRAINT pkey_mt_doc_paymentsaga_tenant_id_id PRIMARY KEY (tenant_id, id)
);
DROP POLICY IF EXISTS marten_tenant_isolation ON payment.mt_doc_paymentsaga;
ALTER TABLE payment.mt_doc_paymentsaga NO FORCE ROW LEVEL SECURITY;
ALTER TABLE payment.mt_doc_paymentsaga DISABLE ROW LEVEL SECURITY;

alter table payment.mt_doc_paymentsummary add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table payment.mt_doc_paymentsummary drop constraint pkey_mt_doc_paymentsummary_id CASCADE;
alter table payment.mt_doc_paymentsummary add CONSTRAINT pkey_mt_doc_paymentsummary_tenant_id_id PRIMARY KEY (tenant_id, id);
CREATE TABLE IF NOT EXISTS security.mt_doc_permissionscopeconfig (
    id                  uuid                        NOT NULL,
    data                jsonb                       NOT NULL,
    mt_last_modified    timestamp with time zone    NULL DEFAULT (transaction_timestamp()),
    mt_version          uuid                        NOT NULL DEFAULT (md5(random()::text || clock_timestamp()::text)::uuid),
    mt_dotnet_type      varchar                     NULL,
CONSTRAINT pkey_mt_doc_permissionscopeconfig_id PRIMARY KEY (id)
);
alter table business.mt_doc_placedstudentview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table business.mt_doc_placedstudentview drop constraint pkey_mt_doc_placedstudentview_id CASCADE;
alter table business.mt_doc_placedstudentview add CONSTRAINT pkey_mt_doc_placedstudentview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table payment.mt_doc_placementview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table payment.mt_doc_placementview drop constraint pkey_mt_doc_placementview_id CASCADE;
alter table payment.mt_doc_placementview add CONSTRAINT pkey_mt_doc_placementview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_schoolplacedstudentview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_schoolplacedstudentview drop constraint pkey_mt_doc_schoolplacedstudentview_id CASCADE;
alter table coordination.mt_doc_schoolplacedstudentview add CONSTRAINT pkey_mt_doc_schoolplacedstudentview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_skillexam add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_skillexam drop constraint pkey_mt_doc_skillexam_id CASCADE;
alter table coordination.mt_doc_skillexam add CONSTRAINT pkey_mt_doc_skillexam_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table payment.mt_doc_studentabsenceview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table payment.mt_doc_studentabsenceview drop constraint pkey_mt_doc_studentabsenceview_id CASCADE;
alter table payment.mt_doc_studentabsenceview add CONSTRAINT pkey_mt_doc_studentabsenceview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table reporting.mt_doc_studentattendancereportview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table reporting.mt_doc_studentattendancereportview drop constraint pkey_mt_doc_studentattendancereportview_id CASCADE;
alter table reporting.mt_doc_studentattendancereportview add CONSTRAINT pkey_mt_doc_studentattendancereportview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table attendance.mt_doc_studentnameview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table attendance.mt_doc_studentnameview drop constraint pkey_mt_doc_studentnameview_id CASCADE;
alter table attendance.mt_doc_studentnameview add CONSTRAINT pkey_mt_doc_studentnameview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table contract.mt_doc_studentnameview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table contract.mt_doc_studentnameview drop constraint pkey_mt_doc_studentnameview_id CASCADE;
alter table contract.mt_doc_studentnameview add CONSTRAINT pkey_mt_doc_studentnameview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table payment.mt_doc_studentpaymentprofile add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table payment.mt_doc_studentpaymentprofile drop constraint pkey_mt_doc_studentpaymentprofile_id CASCADE;
alter table payment.mt_doc_studentpaymentprofile add CONSTRAINT pkey_mt_doc_studentpaymentprofile_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table reporting.mt_doc_studentplacementreportview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table reporting.mt_doc_studentplacementreportview drop constraint pkey_mt_doc_studentplacementreportview_id CASCADE;
alter table reporting.mt_doc_studentplacementreportview add CONSTRAINT pkey_mt_doc_studentplacementreportview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table enrollment.mt_doc_studentprofile add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table enrollment.mt_doc_studentprofile drop constraint pkey_mt_doc_studentprofile_id CASCADE;
alter table enrollment.mt_doc_studentprofile add CONSTRAINT pkey_mt_doc_studentprofile_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_studenttermgrade add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_studenttermgrade drop constraint pkey_mt_doc_studenttermgrade_id CASCADE;
alter table coordination.mt_doc_studenttermgrade add CONSTRAINT pkey_mt_doc_studenttermgrade_tenant_id_id PRIMARY KEY (tenant_id, id);
CREATE TABLE IF NOT EXISTS reporting.mt_doc_studenttermgradeview (
    tenant_id           varchar                     NOT NULL DEFAULT '*DEFAULT*',
    id                  uuid                        NOT NULL,
    data                jsonb                       NOT NULL,
    mt_last_modified    timestamp with time zone    NULL DEFAULT (transaction_timestamp()),
    mt_version          uuid                        NOT NULL DEFAULT (md5(random()::text || clock_timestamp()::text)::uuid),
    mt_dotnet_type      varchar                     NULL,
CONSTRAINT pkey_mt_doc_studenttermgradeview_tenant_id_id PRIMARY KEY (tenant_id, id)
);

CREATE INDEX mt_doc_studenttermgradeview_idx_student_id ON reporting.mt_doc_studenttermgradeview USING btree ((CAST(data ->> 'studentId' as uuid)));

CREATE INDEX idx_term_grade_rpt_student_period ON reporting.mt_doc_studenttermgradeview USING btree ((CAST(data ->> 'studentId' as uuid)), (CAST(data ->> 'academicPeriodId' as uuid)));
DROP POLICY IF EXISTS marten_tenant_isolation ON reporting.mt_doc_studenttermgradeview;
ALTER TABLE reporting.mt_doc_studenttermgradeview NO FORCE ROW LEVEL SECURITY;
ALTER TABLE reporting.mt_doc_studenttermgradeview DISABLE ROW LEVEL SECURITY;

alter table enrollment.mt_doc_teacherprofile add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table enrollment.mt_doc_teacherprofile drop constraint pkey_mt_doc_teacherprofile_id CASCADE;
alter table enrollment.mt_doc_teacherprofile add CONSTRAINT pkey_mt_doc_teacherprofile_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_teacherschedule add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_teacherschedule drop constraint pkey_mt_doc_teacherschedule_id CASCADE;
alter table coordination.mt_doc_teacherschedule add CONSTRAINT pkey_mt_doc_teacherschedule_tenant_id_id PRIMARY KEY (tenant_id, id);
CREATE TABLE IF NOT EXISTS reporting.mt_doc_visitassignmentreportview (
    tenant_id           varchar                     NOT NULL DEFAULT '*DEFAULT*',
    id                  uuid                        NOT NULL,
    data                jsonb                       NOT NULL,
    mt_last_modified    timestamp with time zone    NULL DEFAULT (transaction_timestamp()),
    mt_version          uuid                        NOT NULL DEFAULT (md5(random()::text || clock_timestamp()::text)::uuid),
    mt_dotnet_type      varchar                     NULL,
CONSTRAINT pkey_mt_doc_visitassignmentreportview_tenant_id_id PRIMARY KEY (tenant_id, id)
);

CREATE INDEX idx_visit_rpt_inst_teacher ON reporting.mt_doc_visitassignmentreportview USING btree ((CAST(data ->> 'institutionId' as uuid)), (CAST(data ->> 'teacherId' as uuid)));

CREATE INDEX mt_doc_visitassignmentreportview_idx_visit_date ON reporting.mt_doc_visitassignmentreportview USING btree ((shared.mt_immutable_date(data ->> 'visitDate')));
DROP POLICY IF EXISTS marten_tenant_isolation ON reporting.mt_doc_visitassignmentreportview;
ALTER TABLE reporting.mt_doc_visitassignmentreportview NO FORCE ROW LEVEL SECURITY;
ALTER TABLE reporting.mt_doc_visitassignmentreportview DISABLE ROW LEVEL SECURITY;

alter table coordination.mt_doc_weeklyvisitassignment add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_weeklyvisitassignment drop constraint pkey_mt_doc_weeklyvisitassignment_id CASCADE;
alter table coordination.mt_doc_weeklyvisitassignment add CONSTRAINT pkey_mt_doc_weeklyvisitassignment_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table coordination.mt_doc_weeklyvisitplan add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table coordination.mt_doc_weeklyvisitplan drop constraint pkey_mt_doc_weeklyvisitplan_id CASCADE;
alter table coordination.mt_doc_weeklyvisitplan add CONSTRAINT pkey_mt_doc_weeklyvisitplan_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table attendance.mt_doc_workcalendar add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table attendance.mt_doc_workcalendar drop constraint pkey_mt_doc_workcalendar_id CASCADE;
alter table attendance.mt_doc_workcalendar add CONSTRAINT pkey_mt_doc_workcalendar_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table reporting.mt_doc_workcalendarreportview add column tenant_id varchar NOT NULL DEFAULT '*DEFAULT*';
alter table reporting.mt_doc_workcalendarreportview drop constraint pkey_mt_doc_workcalendarreportview_id CASCADE;
alter table reporting.mt_doc_workcalendarreportview add CONSTRAINT pkey_mt_doc_workcalendarreportview_tenant_id_id PRIMARY KEY (tenant_id, id);
alter table shared.mt_streams drop constraint pkey_mt_streams_id CASCADE;
alter table shared.mt_streams add CONSTRAINT pkey_mt_streams_tenant_id_id PRIMARY KEY (tenant_id, id);
ALTER TABLE shared.mt_events DROP CONSTRAINT IF EXISTS fkey_mt_events_stream_id_tenant_id;
ALTER TABLE shared.mt_events
ADD CONSTRAINT fkey_mt_events_stream_id_tenant_id FOREIGN KEY(tenant_id, stream_id)
REFERENCES shared.mt_streams(tenant_id, id);
drop index if exists shared.pk_mt_events_stream_and_version;
ALTER TABLE shared.mt_events DROP CONSTRAINT IF EXISTS fkey_mt_events_stream_id;
CREATE UNIQUE INDEX pk_mt_events_stream_and_version ON shared.mt_events USING btree (tenant_id, stream_id, version);
DROP FUNCTION IF EXISTS shared.mt_archive_stream(streamid uuid) cascade;

CREATE OR REPLACE FUNCTION shared.mt_archive_stream(streamid uuid, tenantid varchar) RETURNS VOID LANGUAGE plpgsql AS
$function$
BEGIN
  update shared.mt_streams set is_archived = TRUE where id = streamid  and tenant_id = tenantid;
  update shared.mt_events set is_archived = TRUE where stream_id = streamid  and tenant_id = tenantid;
END;
$function$;

DROP FUNCTION IF EXISTS shared.mt_quick_append_events(stream uuid, stream_type character varying, tenantid character varying, event_ids uuid[], event_types character varying[], dotnet_types character varying[], bodies jsonb[], bdatas bytea[], timestamps timestamp with time zone[], expected_version bigint) cascade;

CREATE OR REPLACE FUNCTION shared.mt_quick_append_events(stream uuid, stream_type varchar, tenantid varchar, event_ids uuid[], event_types varchar[], dotnet_types varchar[], bodies jsonb[], bdatas bytea[], timestamps timestamp with time zone[], expected_version bigint DEFAULT NULL::bigint) RETURNS bigint[] AS $$
DECLARE
	event_version bigint;
	stream_is_archived boolean;
	event_type varchar;
	event_id uuid;
	body jsonb;
	index int;
	seq bigint;
    actual_tenant varchar;
	return_value bigint[];
BEGIN
    if expected_version IS NOT NULL then
        -- COALESCE turns the NULL we get for a brand-new stream into 0, so a
        -- FetchForWriting against a non-existent stream (which sets
        -- ExpectedVersionOnServer = 0) and a StartStream(id, version: 0) both
        -- land on the new-stream branch instead of mis-firing the guard.
        select version, is_archived into event_version, stream_is_archived from shared.mt_streams where id = stream AND tenant_id = tenantid;
        if COALESCE(event_version, 0) != expected_version then
            RAISE EXCEPTION 'Stream version mismatch on ''%'': expected %, actual %', stream, expected_version, COALESCE(event_version, 0) USING ERRCODE = 'MT003';
        end if;
    else
        select version, is_archived into event_version, stream_is_archived from shared.mt_streams where id = stream AND tenant_id = tenantid;
    end if;

	if event_version IS NULL then
		event_version = 0;
		insert into shared.mt_streams (id, type, version, timestamp, tenant_id) values (stream, stream_type, 0, now(), tenantid);
    else
        if stream_is_archived then
            RAISE EXCEPTION 'Attempted to append event to archived stream with Id ''%''.', stream USING ERRCODE = 'MT001';
        end if;
        if tenantid IS NOT NULL then
            select tenant_id into actual_tenant from shared.mt_streams where id = stream AND tenant_id = tenantid;
            if actual_tenant != tenantid then
                RAISE EXCEPTION 'The tenantid does not match the existing stream';
            end if;
        end if;
	end if;

	index := 1;
	return_value := ARRAY[event_version + array_length(event_ids, 1)];

	foreach event_id in ARRAY event_ids
	loop
        seq := nextval('shared.mt_events_sequence');
		return_value := array_append(return_value, seq);

	    event_version := event_version + 1;
		event_type = event_types[index];
		body = bodies[index];

		-- #4515 / #4578 / Phase 2: bdatas[index] carries the binary payload
		-- for events opted in to binary serialization (NULL otherwise).
		-- bodies[index] is the {} JSON placeholder for those events so the
		-- existing data jsonb NOT NULL constraint stays intact.
		insert into shared.mt_events
			(seq_id, id, stream_id, version, data, bdata, type, tenant_id, timestamp, mt_dotnet_type, is_archived)
		values
			(seq, event_id, stream, event_version, body, bdatas[index], event_type, tenantid, timestamps[index], dotnet_types[index], FALSE);

		index := index + 1;
	end loop;

	update shared.mt_streams set version = event_version, timestamp = now() where id = stream AND tenant_id = tenantid;

	return return_value;
END
$$ LANGUAGE plpgsql;

