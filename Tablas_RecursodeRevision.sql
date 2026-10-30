USE SistemaSolicitudes;
GO

-- ============================================================================
-- 1. TABLA MAESTRA DEL EXPEDIENTE (Estructurada en los 4 Bloques Procesales)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EXPEDIENTE_RECURSO')
BEGIN
    CREATE TABLE EXPEDIENTE_RECURSO (
        -- IDENTIFICADOR PRINCIPAL
        ID_EXPEDIENTE INT IDENTITY(1,1) PRIMARY KEY,

        -- ==========================================
        -- BLOQUE A: DATOS GENERALES DEL RECURSO
        -- ==========================================
        NUMERO_RECURSO NVARCHAR(50) NOT NULL UNIQUE, -- Ej. RR/0001/2026
        FOLIO_SOLICITUD NVARCHAR(50) NOT NULL,
        MATERIA_RECURSO NVARCHAR(100) NULL, -- 'Acceso a la información (DAI)' o 'Datos personales (DP)'
        NOMBRE_RECURRENTE NVARCHAR(200) NULL,
        FECHA_PRESENTACION DATETIME NULL,
        ESTATUS NVARCHAR(100) NULL, -- 'Admitido', 'En trámite', 'Concluido', etc.
        RESPONSABLE_INTERNO NVARCHAR(150) NULL, -- Usuario asignado

        -- ==========================================
        -- BLOQUE B: DATOS DE LA SOLICITUD ORIGINAL
        -- ==========================================
        CONTENIDO_SOLICITUD NVARCHAR(MAX) NULL,
        FECHA_RESPUESTA_SOLICITUD DATETIME NULL,
        RESPUESTA_OTORGADA NVARCHAR(MAX) NULL,
        RAZON_INTERPOSICION NVARCHAR(500) NULL,

        -- ==========================================
        -- BLOQUE C: TRÁMITE DEL RECURSO
        -- ==========================================
        FECHA_ADMISION DATETIME NULL,
        FECHA_NOTIFICACION_ADMISION DATETIME NULL,
        FECHA_LIMITE_CONTESTACION DATETIME NULL,
        FECHA_CONTESTACION_RECURSO DATETIME NULL,
        SENTIDO_CONTESTACION NVARCHAR(500) NULL,
        FECHA_CIERRE_INSTRUCCION DATETIME NULL,
        FECHA_RESOLUCION DATETIME NULL,
        SENTIDO_RESOLUCION NVARCHAR(200) NULL, -- 'Confirma', 'Revoca', 'Modifica', 'Sobresee'
        FECHA_NOTIFICACION_RESOLUCION DATETIME NULL,

        -- ==========================================
        -- BLOQUE D: CUMPLIMIENTO
        -- ==========================================
        REQUIERE_CUMPLIMIENTO BIT NULL DEFAULT 0, -- 1=Sí, 0=No
        FECHA_LIMITE_CUMPLIMIENTO DATETIME NULL,
        FECHA_CUMPLIO DATETIME NULL,
        CUMPLIO_RESOLUCION NVARCHAR(100) NULL, -- 'Sí', 'No', 'Parcial'
        ACUERDO_CUMPLIMIENTO NVARCHAR(MAX) NULL,
        OBSERVACIONES NVARCHAR(MAX) NULL,

        -- ==========================================
        -- METADATOS DEL SISTEMA
        -- ==========================================
        FECHA_CREACION DATETIME NOT NULL DEFAULT GETDATE(),
        FECHA_MODIFICACION DATETIME NOT NULL DEFAULT GETDATE()
    );
END;
GO

-- ============================================================================
-- 2. TABLA DE GESTIÓN Y VINCULACIÓN DE ARCHIVOS PDF
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EXPEDIENTE_ARCHIVOS')
BEGIN
    CREATE TABLE EXPEDIENTE_ARCHIVOS (
        ID_ARCHIVO INT IDENTITY(1,1) PRIMARY KEY,
        ID_EXPEDIENTE INT NOT NULL,
        NOMBRE_ARCHIVO NVARCHAR(255) NOT NULL,
        CONTENIDO VARBINARY(MAX) NOT NULL,
        TIPO_DOCUMENTO VARCHAR(100) NULL,
        FECHA_DOCUMENTO DATE NULL,
        DESCRIPCION VARCHAR(250) NULL,
        USUARIO_REGISTRO VARCHAR(100) NULL DEFAULT 'Usuario',
        FECHA_REGISTRO DATETIME NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_EXPEDIENTE_ARCHIVOS FOREIGN KEY (ID_EXPEDIENTE) 
            REFERENCES EXPEDIENTE_RECURSO(ID_EXPEDIENTE)
            ON DELETE CASCADE
    );
END;
GO

-- ============================================================================
-- 3. TABLA DE BITÁCORA Y LÍNEA DE TIEMPO PROCESAL DEL EXPEDIENTE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EXPEDIENTE_BITACORA')
BEGIN
    CREATE TABLE EXPEDIENTE_BITACORA (
        ID_ACTUACION INT IDENTITY(1,1) PRIMARY KEY,
        ID_EXPEDIENTE INT NOT NULL,
        FECHA_ACTUACION DATE NOT NULL,
        ACTUACION NVARCHAR(150) NOT NULL, -- 'Registro', 'Admisión', 'Contestación', 'Resolución', etc.
        DESCRIPCION NVARCHAR(500) NOT NULL,
        DOCUMENTO_VINCULADO NVARCHAR(255) NULL, -- Nombre del archivo PDF asociado
        USUARIO NVARCHAR(100) NOT NULL DEFAULT 'Usuario',
        FECHA_REGISTRO DATETIME NOT NULL DEFAULT GETDATE(),

        -- Relación foránea: si se borra el expediente, se limpian sus actuaciones
        CONSTRAINT FK_EXPEDIENTE_BITACORA FOREIGN KEY (ID_EXPEDIENTE) 
            REFERENCES EXPEDIENTE_RECURSO(ID_EXPEDIENTE)
            ON DELETE CASCADE
    );
END;
GO
