import { sql } from '@vercel/postgres';

/**
 * ID 등록 API (관리자 전용)
 * POST /api/register-id
 * Body: { 
 *   "adminKey": "your-admin-key",
 *   "id": "MACHINE-ABC-123",
 *   "expiresAt": "2025-12-31"  // 선택사항
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
    const { adminKey, id, expiresAt } = req.body;

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

    // 만료일 검증 (선택사항)
    if (expiresAt) {
      const expiryDate = new Date(expiresAt);
      if (isNaN(expiryDate.getTime())) {
        return res.status(400).json({ 
          success: false,
          error: 'Invalid expiry date format. Use YYYY-MM-DD' 
        });
      }
    }

    // PostgreSQL에 ID 등록 (UPSERT)
    const result = await sql`
      INSERT INTO licenses (machine_id, valid, expires_at, registered_at)
      VALUES (${id}, true, ${expiresAt || null}, NOW())
      ON CONFLICT (machine_id) 
      DO UPDATE SET 
        valid = true,
        expires_at = ${expiresAt || null},
        updated_at = NOW()
      RETURNING *
    `;

    const license = result.rows[0];

    return res.status(200).json({ 
      success: true,
      message: 'ID registered successfully',
      id: id,
      data: {
        valid: license.valid,
        registeredAt: license.registered_at,
        expiresAt: license.expires_at
      }
    });

  } catch (error) {
    console.error('Registration error:', error);
    return res.status(500).json({ 
      success: false,
      error: 'Internal server error',
      details: error.message
    });
  }
}
