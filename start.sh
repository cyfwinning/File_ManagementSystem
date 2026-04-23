#!/bin/bash
echo "============================================"
echo "  企业智能知识库 - 一键启动"
echo "  浙江南芯半导体"
echo "============================================"
echo ""

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "[1/2] 启动后端 API 服务 (端口 5000)..."
cd "$SCRIPT_DIR/src/KnowledgeBase.WebApi"
dotnet run --urls=http://localhost:5000 &
BACKEND_PID=$!

echo "[2/2] 启动前端开发服务 (端口 3000)..."
cd "$SCRIPT_DIR/frontend/knowledge-base-web"
npm run dev &
FRONTEND_PID=$!

echo ""
echo "服务启动中..."
echo "  后端 API:  http://localhost:5000/swagger"
echo "  前端页面:  http://localhost:3000"
echo "  默认账号:  admin / admin123"
echo ""
echo "按 Ctrl+C 停止服务"

trap "kill $BACKEND_PID $FRONTEND_PID 2>/dev/null; exit" SIGINT SIGTERM
wait
