import { sql } from '@vercel/postgres';

/**
 * 모든 ID 조회 API (관리자 전용)
 * POST /api/list-ids
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

    // PostgreSQL의 jlicense 테이블에서 모든 라이선스 조회 (product, username 포함)
    const result = await sql`
      SELECT 
        machine_id as id,
        product,
        username,
        valid,
        registered_at,
        expires_at,
        updated_at
      FROM jlicense 
      ORDER BY registered_at DESC
    `;

    return res.status(200).json({ 
      success: true,
      count: result.rowCount,
      licenses: result.rows
    });

  } catch (error) {
    console.error('List error:', error);
    return res.status(500).json({ 
      success: false,
      error: 'Internal server error' 
    });
  }
}
