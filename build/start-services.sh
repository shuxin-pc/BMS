#!/bin/bash
echo "启动MES系统服务..."
docker-compose -f docker-compose.services.yml up -d
echo "服务启动完成！"
echo ""
echo "服务访问地址："
echo "API网关: http://localhost:5000"
echo "认证服务: http://localhost:5002"
echo ""
echo "查看服务状态: docker-compose -f docker-compose.services.yml ps"
echo "停止服务: docker-compose -f docker-compose.services.yml down"
echo "查看日志: docker-compose -f docker-compose.services.yml logs -f [服务名]"
