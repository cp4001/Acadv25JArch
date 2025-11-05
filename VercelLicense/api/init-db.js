import { sql } from '@vercel/postgres';

/**
 * 데이터베이스 초기화 API (관리자 전용)
 * 테이블이 없으면 생성합니다
 * POST /api/init-db
 * Body: { 
 *   "adminKey": "your-admin-key"
 * }
 */
export default async function handler(req, res) {
  // CORS 헤더 설정
  res.setHeader('Access-Control-Allow-Credentials', true);
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET,OPTIONS,PATCH,DELETE,POST,PUT');
  res.setHeader(
    'Access-Control-Allow-Headers',
    'X-CSRF-Token, X-Requested-With, Accept, Accept-Version, Content-Length, Content-MD5, Content-Type, Date, X-Api-Version'
  );

  if (req.method === 'OPTIONS') {
    res.status(200).end();
    return;
  }

  if (req.method !== 'POST') {
    return res.status(405).json({ 
      success: false,
      error: 'Method not allowed' 
    });
  }

  try {
    const { adminKey } = req.body;

    // 관리자 키 확인
    if (!adminKey || adminKey !== process.env.ADMIN_KEY) {
      return res.status(403).json({ 
        success: false,
        error: 'Unauthorized - Invalid admin key' 
      });
    }

    // licenses 테이블 생성
    await sql`
      CREATE TABLE IF NOT EXISTS licenses (
        id SERIAL PRIMARY KEY,
        machine_id VARCHAR(255) UNIQUE NOT NULL,
        valid BOOLEAN DEFAULT true,
        registered_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        expires_at DATE,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `;

    // 인덱스 생성 (성능 향상)
    await sql`
      CREATE INDEX IF NOT EXISTS idx_machine_id ON licenses(machine_id)
    `;

    await sql`
      CREATE INDEX IF NOT EXISTS idx_valid ON licenses(valid)
    `;

    return res.status(200).json({ 
      success: true,
      message: 'Database initialized successfully',
      table: 'licenses'
    });

  } catch (error) {
    console.error('Init DB error:', error);
    return res.status(500).json({ 
      success: false,
      error: 'Internal server error',
      details: error.message
    });
  }
}
