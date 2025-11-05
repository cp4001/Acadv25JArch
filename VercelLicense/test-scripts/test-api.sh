#!/bin/bash

# ========================================
# Vercel License Server 테스트 스크립트
# ========================================

# 설정
API_BASE_URL="https://your-project.vercel.app"  # ⚠️ 실제 URL로 변경
ADMIN_KEY="super-secret-admin-key-change-me-12345"  # ⚠️ 실제 키로 변경

echo "========================================"
echo "Vercel License Server 테스트"
echo "========================================"
echo ""

# ========================================
# 1. ID 등록 테스트
# ========================================
echo "1. ID 등록 테스트..."

curl -X POST "$API_BASE_URL/api/register-id" \
  -H "Content-Type: application/json" \
  -d "{
    \"adminKey\": \"$ADMIN_KEY\",
    \"id\": \"MACHINE-TEST-123\",
    \"expiresAt\": \"2025-12-31\"
  }" \
  -w "\n" \
  -s | jq .

echo ""

# ========================================
# 2. 라이선스 확인 테스트 (유효한 ID)
# ========================================
echo "2. 라이선스 확인 테스트 (유효한 ID)..."

curl -X POST "$API_BASE_URL/api/check-license" \
  -H "Content-Type: application/json" \
  -d "{
    \"id\": \"MACHINE-TEST-123\"
  }" \
  -w "\n" \
  -s | jq .

echo ""

# ========================================
# 3. 라이선스 확인 테스트 (잘못된 ID)
# ========================================
echo "3. 라이선스 확인 테스트 (잘못된 ID)..."

curl -X POST "$API_BASE_URL/api/check-license" \
  -H "Content-Type: application/json" \
  -d "{
    \"id\": \"INVALID-ID-999\"
  }" \
  -w "\n" \
  -s | jq .

echo ""

# ========================================
# 4. 모든 ID 조회 테스트
# ========================================
echo "4. 모든 ID 조회 테스트..."

curl -X POST "$API_BASE_URL/api/list-ids" \
  -H "Content-Type: application/json" \
  -d "{
    \"adminKey\": \"$ADMIN_KEY\"
  }" \
  -w "\n" \
  -s | jq .

echo ""

# ========================================
# 5. ID 삭제 테스트
# ========================================
echo "5. ID 삭제 테스트..."

curl -X POST "$API_BASE_URL/api/delete-id" \
  -H "Content-Type: application/json" \
  -d "{
    \"adminKey\": \"$ADMIN_KEY\",
    \"id\": \"MACHINE-TEST-123\"
  }" \
  -w "\n" \
  -s | jq .

echo ""

# ========================================
# 6. 삭제 후 라이선스 확인
# ========================================
echo "6. 삭제 후 라이선스 확인 (실패해야 함)..."

curl -X POST "$API_BASE_URL/api/check-license" \
  -H "Content-Type: application/json" \
  -d "{
    \"id\": \"MACHINE-TEST-123\"
  }" \
  -w "\n" \
  -s | jq .

echo ""
echo "========================================"
echo "테스트 완료!"
echo "========================================"
