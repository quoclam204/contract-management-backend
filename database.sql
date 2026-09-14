IF DB_ID('ContractDb') IS NULL
BEGIN
    CREATE DATABASE ContractDb;
END
GO

USE ContractDb;
GO

/* ============================================================================
   1. DEPARTMENTS - Phong ban
   ============================================================================ */
CREATE TABLE dbo.DEPARTMENTS
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_DEPARTMENTS_Id DEFAULT NEWID(),
    Name        NVARCHAR(200)    NOT NULL,
    ManagerId   UNIQUEIDENTIFIER NULL,          -- FK -> USERS, them constraint sau khi tao USERS
    CreatedAt   DATETIME2        NOT NULL CONSTRAINT DF_DEPARTMENTS_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_DEPARTMENTS PRIMARY KEY CLUSTERED (Id)
);
GO

/* ============================================================================
   2. USERS - Nguoi dung
   Role: 0=Admin, 1=Manager (Truong phong), 2=Staff (Nhan vien), 3=Approver
   ============================================================================ */
CREATE TABLE dbo.USERS
(
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_USERS_Id DEFAULT NEWID(),
    FullName        NVARCHAR(200)    NOT NULL,
    Email           NVARCHAR(256)    NOT NULL,
    PasswordHash    NVARCHAR(512)    NOT NULL,
    Role            TINYINT          NOT NULL,
    DepartmentId    UNIQUEIDENTIFIER NULL,
    IsActive        BIT              NOT NULL CONSTRAINT DF_USERS_IsActive DEFAULT 1,
    CreatedAt       DATETIME2        NOT NULL CONSTRAINT DF_USERS_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2        NULL,

    CONSTRAINT PK_USERS PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_USERS_Email UNIQUE (Email),
    CONSTRAINT CK_USERS_Role CHECK (Role BETWEEN 0 AND 3),
    CONSTRAINT FK_USERS_DEPARTMENTS FOREIGN KEY (DepartmentId)
        REFERENCES dbo.DEPARTMENTS (Id)
);
GO

-- Bo sung FK con thieu cua DEPARTMENTS.ManagerId (tranh vong lap khi tao)
ALTER TABLE dbo.DEPARTMENTS
    ADD CONSTRAINT FK_DEPARTMENTS_USERS_Manager FOREIGN KEY (ManagerId)
        REFERENCES dbo.USERS (Id);
GO

CREATE INDEX IX_USERS_DepartmentId ON dbo.USERS (DepartmentId);
GO

/* ============================================================================
   3. PARTNERS - Khach hang / Doi tac
   ============================================================================ */
CREATE TABLE dbo.PARTNERS
(
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_PARTNERS_Id DEFAULT NEWID(),
    Name            NVARCHAR(300)    NOT NULL,
    TaxCode         NVARCHAR(50)     NULL,
    Representative  NVARCHAR(200)    NULL,
    ContactEmail    NVARCHAR(256)    NULL,
    Address         NVARCHAR(500)    NULL,
    CreatedAt       DATETIME2        NOT NULL CONSTRAINT DF_PARTNERS_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_PARTNERS PRIMARY KEY CLUSTERED (Id)
);
GO

CREATE UNIQUE INDEX UX_PARTNERS_TaxCode ON dbo.PARTNERS (TaxCode)
    WHERE TaxCode IS NOT NULL;   -- filtered unique index, cho phep nhieu NULL
GO

/* ============================================================================
   4. WORKFLOW_DEFINITIONS - Cau hinh luong duyet (doc lap voi CONTRACT_TYPES,
      duoc gan vao tung phien ban Template thong qua CONTRACT_TEMPLATE_VERSIONS)
   ============================================================================ */
CREATE TABLE dbo.WORKFLOW_DEFINITIONS
(
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_WFDEF_Id DEFAULT NEWID(),
    Name                NVARCHAR(200)    NOT NULL,
    ConditionExpression NVARCHAR(500)    NULL,       -- vd: "Value >= 500000000"
    Version             INT              NOT NULL CONSTRAINT DF_WFDEF_Version DEFAULT 1,
    IsActive            BIT              NOT NULL CONSTRAINT DF_WFDEF_IsActive DEFAULT 1,
    CreatedAt           DATETIME2        NOT NULL CONSTRAINT DF_WFDEF_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_WORKFLOW_DEFINITIONS PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_WFDEF_Name_Version UNIQUE (Name, Version)
);
GO

-- Chi cho phep 1 ban IsActive = 1 tren moi (Name) tai mot thoi diem
CREATE UNIQUE INDEX UX_WFDEF_Name_Active ON dbo.WORKFLOW_DEFINITIONS (Name)
    WHERE IsActive = 1;
GO

/* ============================================================================
   5. WORKFLOW_STEPS - Cau hinh buoc duyet
   ApproverRole: dung chung enum voi USERS.Role (0=Admin,1=Manager,2=Staff,3=Approver)
   ============================================================================ */
CREATE TABLE dbo.WORKFLOW_STEPS
(
    Id                      UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_WFSTEP_Id DEFAULT NEWID(),
    WorkflowDefinitionId    UNIQUEIDENTIFIER NOT NULL,
    StepOrder               INT              NOT NULL,
    ApproverRole            TINYINT          NOT NULL,
    IsRequired              BIT              NOT NULL CONSTRAINT DF_WFSTEP_IsRequired DEFAULT 1,
    MinimumAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_WFSTEP_MinimumAmount DEFAULT 0,

    CONSTRAINT PK_WORKFLOW_STEPS PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_WORKFLOW_STEPS_WORKFLOW_DEFINITIONS FOREIGN KEY (WorkflowDefinitionId)
        REFERENCES dbo.WORKFLOW_DEFINITIONS (Id) ON DELETE CASCADE,
    CONSTRAINT CK_WORKFLOW_STEPS_ApproverRole CHECK (ApproverRole BETWEEN 0 AND 3),
    CONSTRAINT UQ_WORKFLOW_STEPS_Order UNIQUE (WorkflowDefinitionId, StepOrder)
);
GO

CREATE INDEX IX_WORKFLOW_STEPS_WorkflowDefinitionId ON dbo.WORKFLOW_STEPS (WorkflowDefinitionId);
GO

/* ============================================================================
   6. CONTRACT_TYPES - Loai hop dong (chi la danh muc phan loai, KHONG con
      giu noi dung mau hay version - xem CONTRACT_TEMPLATE_VERSIONS ben duoi)
   ============================================================================ */
CREATE TABLE dbo.CONTRACT_TYPES
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CTYPE_Id DEFAULT NEWID(),
    Name        NVARCHAR(200)    NOT NULL,
    CreatedAt   DATETIME2        NOT NULL CONSTRAINT DF_CTYPE_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_CONTRACT_TYPES PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_CONTRACT_TYPES_Name UNIQUE (Name)
);
GO

/* ============================================================================
   7. CONTRACT_TEMPLATE_VERSIONS - Phien ban mau hop dong (versioning THAT)
   Moi lan doi noi dung mau/workflow ap dung -> INSERT row moi, KHONG UPDATE
   tai cho, de giu duoc lich su: hop dong cu van tro dung ve ban mau da dung
   khi tao, hop dong moi tao sau nay se dung ban IsActive = 1 moi nhat.
   ============================================================================ */
CREATE TABLE dbo.CONTRACT_TEMPLATE_VERSIONS
(
    Id                      UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CTV_Id DEFAULT NEWID(),
    ContractTypeId          UNIQUEIDENTIFIER NOT NULL,
    Version                 INT              NOT NULL,
    TemplateFileUrl         NVARCHAR(1000)   NULL,        -- file mau goc (docx/pdf) tren Object Storage
    ContentJson             NVARCHAR(MAX)    NULL,        -- cau truc field/dieu khoan cua mau, dang JSON
    WorkflowDefinitionId    UNIQUEIDENTIFIER NULL,         -- workflow ap dung cho phien ban mau nay
    IsActive                BIT              NOT NULL CONSTRAINT DF_CTV_IsActive DEFAULT 1,
    CreatedBy               UNIQUEIDENTIFIER NOT NULL,
    CreatedAt               DATETIME2        NOT NULL CONSTRAINT DF_CTV_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_CONTRACT_TEMPLATE_VERSIONS PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CTV_CONTRACT_TYPES FOREIGN KEY (ContractTypeId)
        REFERENCES dbo.CONTRACT_TYPES (Id),
    CONSTRAINT FK_CTV_WORKFLOW_DEFINITIONS FOREIGN KEY (WorkflowDefinitionId)
        REFERENCES dbo.WORKFLOW_DEFINITIONS (Id),
    CONSTRAINT FK_CTV_USERS_CreatedBy FOREIGN KEY (CreatedBy)
        REFERENCES dbo.USERS (Id),
    CONSTRAINT UQ_CTV_ContractType_Version UNIQUE (ContractTypeId, Version),
    CONSTRAINT CK_CTV_ContentJson_IsJson CHECK (ContentJson IS NULL OR ISJSON(ContentJson) = 1)
);
GO

-- Chi cho phep 1 phien ban IsActive = 1 cho moi loai hop dong tai mot thoi diem
CREATE UNIQUE INDEX UX_CTV_ContractType_Active ON dbo.CONTRACT_TEMPLATE_VERSIONS (ContractTypeId)
    WHERE IsActive = 1;
GO

CREATE INDEX IX_CTV_WorkflowDefinitionId ON dbo.CONTRACT_TEMPLATE_VERSIONS (WorkflowDefinitionId);
GO

/* ============================================================================
   8. CONTRACTS - Bang trung tam
   Status: 0=Draft,1=PendingApproval,2=Approved,3=Signed,4=Active,
           5=Expiring,6=Renewed,7=Terminated
   ============================================================================ */
CREATE TABLE dbo.CONTRACTS
(
    Id                      UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CONTRACTS_Id DEFAULT NEWID(),
    ContractNumber          NVARCHAR(50)     NOT NULL,
    ContractTypeId          UNIQUEIDENTIFIER NOT NULL,
    TemplateVersionUsedId   UNIQUEIDENTIFIER NOT NULL,      -- FK -> CONTRACT_TEMPLATE_VERSIONS (snapshot ban mau da dung)
    PartnerId               UNIQUEIDENTIFIER NOT NULL,
    OwnerId                 UNIQUEIDENTIFIER NOT NULL,
    Title                   NVARCHAR(500)    NOT NULL,
    Value                   DECIMAL(18,2)    NOT NULL CONSTRAINT DF_CONTRACTS_Value DEFAULT 0,
    SignedDate              DATETIME2        NULL,
    EffectiveDate           DATETIME2        NOT NULL,
    ExpiryDate              DATETIME2        NOT NULL,
    Status                  TINYINT          NOT NULL CONSTRAINT DF_CONTRACTS_Status DEFAULT 0,
    FileUrl                 NVARCHAR(1000)   NULL,
    ParentContractId        UNIQUEIDENTIFIER NULL,      -- tro ve hop dong goc (phu luc gia han)
    CreatedAt               DATETIME2        NOT NULL CONSTRAINT DF_CONTRACTS_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt               DATETIME2        NULL,
    RowVersion              ROWVERSION,                  -- optimistic concurrency (chong sua de len nhau)

    CONSTRAINT PK_CONTRACTS PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_CONTRACTS_ContractNumber UNIQUE (ContractNumber),
    CONSTRAINT FK_CONTRACTS_CONTRACT_TYPES FOREIGN KEY (ContractTypeId)
        REFERENCES dbo.CONTRACT_TYPES (Id),
    CONSTRAINT FK_CONTRACTS_TEMPLATE_VERSIONS FOREIGN KEY (TemplateVersionUsedId)
        REFERENCES dbo.CONTRACT_TEMPLATE_VERSIONS (Id),
    CONSTRAINT FK_CONTRACTS_PARTNERS FOREIGN KEY (PartnerId)
        REFERENCES dbo.PARTNERS (Id),
    CONSTRAINT FK_CONTRACTS_USERS_Owner FOREIGN KEY (OwnerId)
        REFERENCES dbo.USERS (Id),
    CONSTRAINT FK_CONTRACTS_ParentContract FOREIGN KEY (ParentContractId)
        REFERENCES dbo.CONTRACTS (Id),
    CONSTRAINT CK_CONTRACTS_Status CHECK (Status BETWEEN 0 AND 7),
    CONSTRAINT CK_CONTRACTS_ExpiryAfterEffective CHECK (ExpiryDate >= EffectiveDate)
);
GO

CREATE INDEX IX_CONTRACTS_PartnerId ON dbo.CONTRACTS (PartnerId);
CREATE INDEX IX_CONTRACTS_OwnerId ON dbo.CONTRACTS (OwnerId);
CREATE INDEX IX_CONTRACTS_ContractTypeId ON dbo.CONTRACTS (ContractTypeId);
CREATE INDEX IX_CONTRACTS_TemplateVersionUsedId ON dbo.CONTRACTS (TemplateVersionUsedId);
CREATE INDEX IX_CONTRACTS_ParentContractId ON dbo.CONTRACTS (ParentContractId);
-- Index phuc vu job Hangfire quet hop dong sap het han (FR-08) va dashboard loc theo status
CREATE INDEX IX_CONTRACTS_Status_ExpiryDate ON dbo.CONTRACTS (Status, ExpiryDate)
    INCLUDE (ContractNumber, Title, Value, OwnerId);
GO

/* ============================================================================
   9. APPROVAL_STEPS - Buoc phe duyet (runtime, snapshot theo WorkflowDefinition
      da duoc chon tu CONTRACT_TEMPLATE_VERSIONS tai thoi diem submit)
   Decision: 0=Pending, 1=Approved, 2=Rejected
   ============================================================================ */
CREATE TABLE dbo.APPROVAL_STEPS
(
    Id                      UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_APPSTEP_Id DEFAULT NEWID(),
    ContractId              UNIQUEIDENTIFIER NOT NULL,
    WorkflowDefinitionId    UNIQUEIDENTIFIER NOT NULL,
    ApproverId              UNIQUEIDENTIFIER NOT NULL,
    StepOrder               INT              NOT NULL,
    Decision                TINYINT          NOT NULL CONSTRAINT DF_APPSTEP_Decision DEFAULT 0,
    Comment                 NVARCHAR(1000)   NULL,
    DecidedAt               DATETIME2        NULL,
    CreatedAt               DATETIME2        NOT NULL CONSTRAINT DF_APPSTEP_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_APPROVAL_STEPS PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_APPROVAL_STEPS_CONTRACTS FOREIGN KEY (ContractId)
        REFERENCES dbo.CONTRACTS (Id) ON DELETE CASCADE,
    CONSTRAINT FK_APPROVAL_STEPS_WORKFLOW_DEFINITIONS FOREIGN KEY (WorkflowDefinitionId)
        REFERENCES dbo.WORKFLOW_DEFINITIONS (Id),
    CONSTRAINT FK_APPROVAL_STEPS_USERS FOREIGN KEY (ApproverId)
        REFERENCES dbo.USERS (Id),
    CONSTRAINT CK_APPROVAL_STEPS_Decision CHECK (Decision BETWEEN 0 AND 2)
);
GO

CREATE INDEX IX_APPROVAL_STEPS_ContractId ON dbo.APPROVAL_STEPS (ContractId);
CREATE INDEX IX_APPROVAL_STEPS_ApproverId ON dbo.APPROVAL_STEPS (ApproverId);
GO

/* ============================================================================
   10. SIGNATURES - Chu ky dien tu
   SignatureMethod: 0=Mock, 1=OTP, 2=DigitalCA (Stretch Goal)
   SignerType:      0=InternalUser, 1=PartnerRepresentative
   Nguoi ky co the la nhan vien/nguoi co tham quyen NOI BO (FK -> USERS)
   hoac dai dien cua DOI TAC (FK -> PARTNERS) - dung dung 1 trong 2 cot
   theo SignerType, cot con lai phai NULL (rang buoc bang CHECK).
   ============================================================================ */
CREATE TABLE dbo.SIGNATURES
(
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SIG_Id DEFAULT NEWID(),
    ContractId          UNIQUEIDENTIFIER NOT NULL,
    SignerType          TINYINT          NOT NULL,
    InternalSignerId    UNIQUEIDENTIFIER NULL,       -- FK -> USERS, dung khi SignerType = 0
    PartnerSignerId     UNIQUEIDENTIFIER NULL,       -- FK -> PARTNERS, dung khi SignerType = 1
    SignerNameSnapshot  NVARCHAR(200)    NOT NULL,   -- luu ten nguoi ky tai thoi diem ky (dai dien Partner co the khong co tai khoan he thong)
    SignatureMethod     TINYINT          NOT NULL,
    SignedAt            DATETIME2        NOT NULL CONSTRAINT DF_SIG_SignedAt DEFAULT SYSUTCDATETIME(),
    SignatureHash       NVARCHAR(512)    NOT NULL,

    CONSTRAINT PK_SIGNATURES PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_SIGNATURES_CONTRACTS FOREIGN KEY (ContractId)
        REFERENCES dbo.CONTRACTS (Id) ON DELETE CASCADE,
    CONSTRAINT FK_SIGNATURES_USERS FOREIGN KEY (InternalSignerId)
        REFERENCES dbo.USERS (Id),
    CONSTRAINT FK_SIGNATURES_PARTNERS FOREIGN KEY (PartnerSignerId)
        REFERENCES dbo.PARTNERS (Id),
    CONSTRAINT CK_SIGNATURES_Method CHECK (SignatureMethod BETWEEN 0 AND 2),
    CONSTRAINT CK_SIGNATURES_SignerType CHECK (SignerType BETWEEN 0 AND 1),
    CONSTRAINT CK_SIGNATURES_SignerExclusive CHECK (
        (SignerType = 0 AND InternalSignerId IS NOT NULL AND PartnerSignerId IS NULL) OR
        (SignerType = 1 AND PartnerSignerId IS NOT NULL AND InternalSignerId IS NULL)
    )
);
GO

CREATE INDEX IX_SIGNATURES_ContractId ON dbo.SIGNATURES (ContractId);
GO

/* ============================================================================
   11. PAYMENTS - Dot thanh toan
   Status: 0=Pending, 1=Paid, 2=Overdue
   ============================================================================ */
CREATE TABLE dbo.PAYMENTS
(
    Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_PAY_Id DEFAULT NEWID(),
    ContractId    UNIQUEIDENTIFIER NOT NULL,
    Description   NVARCHAR(500)    NULL,
    Amount        DECIMAL(18,2)    NOT NULL,
    DueDate       DATETIME2        NOT NULL,
    Status        TINYINT          NOT NULL CONSTRAINT DF_PAY_Status DEFAULT 0,

    CONSTRAINT PK_PAYMENTS PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_PAYMENTS_CONTRACTS FOREIGN KEY (ContractId)
        REFERENCES dbo.CONTRACTS (Id) ON DELETE CASCADE,
    CONSTRAINT CK_PAYMENTS_Status CHECK (Status BETWEEN 0 AND 2),
    CONSTRAINT CK_PAYMENTS_Amount CHECK (Amount >= 0)
);
GO

CREATE INDEX IX_PAYMENTS_ContractId ON dbo.PAYMENTS (ContractId);
CREATE INDEX IX_PAYMENTS_Status_DueDate ON dbo.PAYMENTS (Status, DueDate);
GO

/* ============================================================================
   12. ATTACHMENTS - Tep dinh kem & versioning (file scan/dinh kem, KHAC voi
       CONTRACT_TEMPLATE_VERSIONS o tren la ban mau goc chua co du lieu)
   ============================================================================ */
CREATE TABLE dbo.ATTACHMENTS
(
    Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ATT_Id DEFAULT NEWID(),
    ContractId   UNIQUEIDENTIFIER NOT NULL,
    FileName     NVARCHAR(500)    NOT NULL,
    Version      INT              NOT NULL CONSTRAINT DF_ATT_Version DEFAULT 1,
    FileUrl      NVARCHAR(1000)   NOT NULL,
    UploadedBy   UNIQUEIDENTIFIER NOT NULL,
    UploadedAt   DATETIME2        NOT NULL CONSTRAINT DF_ATT_UploadedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_ATTACHMENTS PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ATTACHMENTS_CONTRACTS FOREIGN KEY (ContractId)
        REFERENCES dbo.CONTRACTS (Id) ON DELETE CASCADE,
    CONSTRAINT FK_ATTACHMENTS_USERS FOREIGN KEY (UploadedBy)
        REFERENCES dbo.USERS (Id),
    CONSTRAINT UQ_ATTACHMENTS_ContractFileVersion UNIQUE (ContractId, FileName, Version)
);
GO

CREATE INDEX IX_ATTACHMENTS_ContractId ON dbo.ATTACHMENTS (ContractId);
GO

/* ============================================================================
   13. AI_ANALYSIS_RESULTS - Ket qua AI phan tich
   ============================================================================ */
CREATE TABLE dbo.AI_ANALYSIS_RESULTS
(
    Id                    UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_AI_Id DEFAULT NEWID(),
    ContractId            UNIQUEIDENTIFIER NOT NULL,
    Summary               NVARCHAR(MAX)    NULL,
    ExtractedValue        DECIMAL(18,2)    NULL,
    ExtractedExpiryDate   DATETIME2        NULL,
    RiskFlags             NVARCHAR(MAX)    NULL,   -- JSON array
    AnalyzedAt            DATETIME2        NOT NULL CONSTRAINT DF_AI_AnalyzedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_AI_ANALYSIS_RESULTS PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_AI_ANALYSIS_RESULTS_CONTRACTS FOREIGN KEY (ContractId)
        REFERENCES dbo.CONTRACTS (Id) ON DELETE CASCADE,
    CONSTRAINT CK_AI_RiskFlags_IsJson CHECK (RiskFlags IS NULL OR ISJSON(RiskFlags) = 1)
);
GO

CREATE INDEX IX_AI_ANALYSIS_RESULTS_ContractId ON dbo.AI_ANALYSIS_RESULTS (ContractId);
GO

/* ============================================================================
   14. NOTIFICATIONS - Thong bao
   Type: 0=ApprovalRequest, 1=SignRequest, 2=ExpiringSoon, 3=SystemAnnouncement
   ContractId de NULL de ho tro thong bao he thong chung (vd: bao tri).
   ============================================================================ */
CREATE TABLE dbo.NOTIFICATIONS
(
    Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_NOTI_Id DEFAULT NEWID(),
    UserId       UNIQUEIDENTIFIER NOT NULL,
    ContractId   UNIQUEIDENTIFIER NULL,
    Type         TINYINT          NOT NULL,
    IsRead       BIT              NOT NULL CONSTRAINT DF_NOTI_IsRead DEFAULT 0,
    CreatedAt    DATETIME2        NOT NULL CONSTRAINT DF_NOTI_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_NOTIFICATIONS PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_NOTIFICATIONS_USERS FOREIGN KEY (UserId)
        REFERENCES dbo.USERS (Id),
    CONSTRAINT FK_NOTIFICATIONS_CONTRACTS FOREIGN KEY (ContractId)
        REFERENCES dbo.CONTRACTS (Id),
    CONSTRAINT CK_NOTIFICATIONS_Type CHECK (Type BETWEEN 0 AND 3)
);
GO

-- Index phuc vu man hinh "thong bao chua doc cua toi", sap xep moi nhat truoc
CREATE INDEX IX_NOTIFICATIONS_UserId_IsRead ON dbo.NOTIFICATIONS (UserId, IsRead, CreatedAt DESC);
GO

/* ============================================================================
   15. AUDIT_LOGS - Nhat ky kiem toan (BAT BIEN)
   Action: 0=Create, 1=Update, 2=Approve, 3=Sign, 4=Terminate
   Dung BIGINT IDENTITY vi day la log tang dan, khong can GUID random.
   ============================================================================ */
CREATE TABLE dbo.AUDIT_LOGS
(
    Id              BIGINT           NOT NULL IDENTITY(1,1),
    EntityName      NVARCHAR(100)    NOT NULL,
    EntityId        UNIQUEIDENTIFIER NOT NULL,
    Action          TINYINT          NOT NULL,
    PerformedBy     UNIQUEIDENTIFIER NOT NULL,
    PerformedAt     DATETIME2        NOT NULL CONSTRAINT DF_AUDIT_PerformedAt DEFAULT SYSUTCDATETIME(),
    DataSnapshot    NVARCHAR(MAX)    NULL,   -- JSON: anh chup du lieu truoc/sau

    CONSTRAINT PK_AUDIT_LOGS PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_AUDIT_LOGS_USERS FOREIGN KEY (PerformedBy)
        REFERENCES dbo.USERS (Id),
    CONSTRAINT CK_AUDIT_LOGS_Action CHECK (Action BETWEEN 0 AND 4),
    CONSTRAINT CK_AUDIT_DataSnapshot_IsJson CHECK (DataSnapshot IS NULL OR ISJSON(DataSnapshot) = 1)
);
GO

CREATE INDEX IX_AUDIT_LOGS_Entity ON dbo.AUDIT_LOGS (EntityName, EntityId, PerformedAt DESC);
GO

-- Bat bien: chan UPDATE va DELETE tren AUDIT_LOGS o tang database (defense-in-depth,
-- ngoai viec kiem soat o tang Application). Chi cho phep INSERT.
CREATE TRIGGER TR_AUDIT_LOGS_PreventModify
ON dbo.AUDIT_LOGS
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    RAISERROR ('AUDIT_LOGS la bang bat bien: khong cho phep UPDATE/DELETE.', 16, 1);
    ROLLBACK TRANSACTION;
END;
GO

/* ============================================================================
   HET SCRIPT

   So do thu tu tao bang (dam bao khong loi FK):
   DEPARTMENTS -> USERS -> (ALTER DEPARTMENTS.ManagerId) -> PARTNERS ->
   WORKFLOW_DEFINITIONS -> WORKFLOW_STEPS -> CONTRACT_TYPES ->
   CONTRACT_TEMPLATE_VERSIONS -> CONTRACTS -> APPROVAL_STEPS -> SIGNATURES ->
   PAYMENTS -> ATTACHMENTS -> AI_ANALYSIS_RESULTS -> NOTIFICATIONS -> AUDIT_LOGS

   Goi y trien khai (EF Core Migrations):
   - Moi module (theo phan cong o muc 8 SRS) nen co rieng
     DbContext/Configuration class, nhung dung chung 1 database (schema dbo),
     de sau nay de tach thanh service rieng neu can (Modular Monolith).
   - Khi tao Contract moi: query CONTRACT_TEMPLATE_VERSIONS WHERE
     ContractTypeId = @x AND IsActive = 1 de lay ban mau + workflow hien
     hanh, luu Id do vao CONTRACTS.TemplateVersionUsedId - TUYET DOI khong
     cho phep sua lai ban CONTRACT_TEMPLATE_VERSIONS da bi tham chieu boi
     hop dong nao do (chi tao ban moi + IsActive = 1, ban cu tu dong thanh
     IsActive = 0 trong cung 1 transaction).
   - Tuong tu, khi doi WORKFLOW_DEFINITIONS: tao ban moi voi Version + 1,
     roi cap nhat CONTRACT_TEMPLATE_VERSIONS moi tro toi WorkflowDefinitionId
     moi - hop dong dang xu ly (APPROVAL_STEPS da snapshot WorkflowDefinitionId
     cu) khong bi anh huong.
   - RowVersion tren CONTRACTS ho tro optimistic concurrency, tranh 2 nguoi
     cung sua 1 hop dong ghi de len nhau.
   - Co the can them index loc theo Status/CreatedAt tuy theo query thuc te
     phat sinh khi profile hieu nang (NFR muc 1.3).
   ============================================================================ */