import { sql } from '@vercel/postgres';

/**
 * ID 삭제 API (관리자 전용)
 * POST /api/delete-id
 * Body: { 
 *   "adminKey": "your-admin-key",
 *   "id": "MACHINE-ABC-123"
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
    const { adminKey, id } = req.body;

    // 관리자 키 확인
    if (!adminKey || adminKey !== process.env.ADMIN_KEY) {
      return res.status(403).json({ 
        success: false,
        error: 'Unauthorized - Invalid admin key' 
      });
    }

    // ID 검증
    if (!id || typeof id !== 'string') {
      return res.status(400).json({ 
        success: false,
        error: 'Invalid ID format' 
      });
    }

    // PostgreSQL에서 ID 삭제
    const result = await sql`
      DELETE FROM licenses 
      WHERE machine_id = ${id}
      RETURNING *
    `;

    if (result.rowCount === 0) {
      return res.status(404).json({ 
        success: false,
        error: 'ID not found' 
      });
    }

    return res.status(200).json({ 
      success: true,
      message: 'ID deleted successfully',
      id: id
    });

  } catch (error) {
    console.error('Deletion error:', error);
    return res.status(500).json({ 
      success: false,
      error: 'Internal server error' 
    });
  }
}
