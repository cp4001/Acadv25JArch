import { sql } from '@vercel/postgres';

/**
 * 라이선스 확인 API
 * POST /api/check-license
 * Body: { "id": "MACHINE-ABC-123" }
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
    const { id } = req.body;

    if (!id || typeof id !== 'string') {
      return res.status(400).json({ 
        success: false,
        error: 'Invalid ID format' 
      });
    }

    // PostgreSQL의 jlicense 테이블에서 ID 확인
    const result = await sql`
      SELECT * FROM jlicense 
      WHERE machine_id = ${id} 
      AND valid = true
    `;

    if (result.rows.length === 0) {
      return res.status(403).json({ 
        success: false,
        error: 'License not found',
        valid: false
      });
    }

    const license = result.rows[0];

    // 만료일 확인
    if (license.expires_at) {
      const expiryDate = new Date(license.expires_at);
      const now = new Date();
      
      if (now > expiryDate) {
        return res.status(403).json({ 
          success: false,
          error: 'License expired',
          valid: false,
          expiresAt: license.expires_at
        });
      }
    }

    // 유효한 라이선스 - 암호화 키 및 추가 정보 반환
    return res.status(200).json({ 
      success: true,
      valid: true,
      key: process.env.ENCRYPTION_KEY || 'YourSecretKey123',
      expiresAt: license.expires_at,
      registeredAt: license.registered_at,
      username: license.username,
      product: license.product
    });

  } catch (error) {
    console.error('License check error:', error);
    return res.status(500).json({ 
      success: false,
      error: 'Internal server error' 
    });
  }
}
