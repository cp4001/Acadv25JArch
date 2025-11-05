import { sql } from '@vercel/postgres';

/**
 * ID 수정 API (관리자 전용)
 * POST /api/update-id
 * Body: { 
 *   "adminKey": "your-admin-key",
 *   "id": "MACHINE-ABC-123",       // 기존 ID (WHERE 조건)
 *   "product": "ProductName",       // 선택사항
 *   "username": "UserName",         // 선택사항
 *   "expiresAt": "2025-12-31"       // 선택사항
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
    const { adminKey, id, product, username, expiresAt } = req.body;

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

    // PostgreSQL의 jlicense 테이블에서 ID 업데이트
    const result = await sql`
      UPDATE jlicense
      SET 
        product = ${product || null},
        username = ${username || null},
        expires_at = ${expiresAt || null},
        updated_at = NOW()
      WHERE machine_id = ${id}
      RETURNING *
    `;

    // 업데이트된 행이 없으면 ID가 존재하지 않음
    if (result.rowCount === 0) {
      return res.status(404).json({ 
        success: false,
        error: 'License not found',
        details: 'The specified machine ID does not exist'
      });
    }

    const license = result.rows[0];

    return res.status(200).json({ 
      success: true,
      message: 'License updated successfully',
      id: id,
      data: {
        product: license.product,
        username: license.username,
        valid: license.valid,
        registeredAt: license.registered_at,
        expiresAt: license.expires_at,
        updatedAt: license.updated_at
      }
    });

  } catch (error) {
    console.error('Update error:', error);
    
    return res.status(500).json({ 
      success: false,
      error: 'Internal server error',
      details: error.message
    });
  }
}
