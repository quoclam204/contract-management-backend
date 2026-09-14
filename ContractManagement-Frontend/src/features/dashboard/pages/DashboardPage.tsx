import React, { useEffect, useState } from 'react';
import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  ResponsiveContainer,
  Legend,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  BarChart,
  Bar,
} from 'recharts';
import { dashboardApi } from '../services/dashboardApi';
import type {
  DashboardSummaryDto,
  ContractStatusSummaryDto,
  DepartmentContractSummaryDto,
  PartnerContractSummaryDto,
  MonthlyContractSummaryDto,
} from '../types/dashboard.types';

const STATUS_COLORS = [
  '#94a3b8', // 0: Draft - slate
  '#f59e0b', // 1: PendingApproval - amber
  '#0284c7', // 2: Approved - sky
  '#6366f1', // 3: Signed - indigo
  '#10b981', // 4: Active - emerald
  '#ea580c', // 5: Expiring - orange
  '#2563eb', // 6: Renewed - blue
  '#e11d48', // 7: Terminated - rose
];

export const DashboardPage: React.FC = () => {
  const [summary, setSummary] = useState<DashboardSummaryDto | null>(null);
  const [statusData, setStatusData] = useState<ContractStatusSummaryDto[]>([]);
  const [deptData, setDeptData] = useState<DepartmentContractSummaryDto[]>([]);
  const [partnerData, setPartnerData] = useState<PartnerContractSummaryDto[]>([]);
  const [trendData, setTrendData] = useState<MonthlyContractSummaryDto[]>([]);

  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchDashboardData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [sumRes, statusRes, deptRes, partnerRes, trendRes] = await Promise.all([
        dashboardApi.getSummary(),
        dashboardApi.getByStatus(),
        dashboardApi.getByDepartment(),
        dashboardApi.getByPartner(5),
        dashboardApi.getByTime(),
      ]);
      setSummary(sumRes);
      setStatusData(statusRes);
      setDeptData(deptRes);
      setPartnerData(partnerRes);
      setTrendData(trendRes);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Không thể tải dữ liệu dashboard');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchDashboardData();
  }, []);

  const formatCurrency = (val?: number) => {
    if (val === undefined || val === null) return '0 ₫';
    return `${val.toLocaleString('vi-VN')} ₫`;
  };

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[450px] space-y-3">
        <div className="w-10 h-10 border-4 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
        <p className="text-gray-500 text-sm">Đang tải số liệu tổng quan...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-6">
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm flex items-center justify-between">
          <span>{error}</span>
          <button
            onClick={fetchDashboardData}
            className="px-3 py-1 bg-red-600 text-white rounded text-xs font-semibold hover:bg-red-700"
          >
            Thử lại
          </button>
        </div>
      </div>
    );
  }

  const pieChartData = statusData.map((item) => ({
    name: item.statusName || `Trạng thái ${item.status}`,
    value: item.count,
    totalValue: item.totalValue,
  }));

  const trendChartData = trendData.map((item) => ({
    label: `T${item.month}/${item.year}`,
    count: item.count,
    totalValue: item.totalValue,
  }));

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 m-0">Tổng quan hệ thống</h1>
          <p className="text-xs text-gray-500 mt-1">
            Theo dõi tình hình hợp đồng, luồng phê duyệt và báo cáo thống kê theo thời gian thực
          </p>
        </div>
        <button
          onClick={fetchDashboardData}
          className="inline-flex items-center px-3 py-1.5 bg-white border border-gray-300 rounded-lg text-xs font-medium text-gray-700 hover:bg-gray-50 shadow-sm"
        >
          🔄 Làm mới dữ liệu
        </button>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white p-5 rounded-xl border border-gray-200 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-gray-500 uppercase">Tổng số hợp đồng</span>
            <span className="p-2 bg-blue-50 text-blue-600 rounded-lg text-lg">📄</span>
          </div>
          <div className="mt-3">
            <span className="text-2xl font-bold text-gray-900">{summary?.totalContracts ?? 0}</span>
            <p className="text-xs text-gray-500 mt-1">Tổng giá trị: <span className="font-semibold text-gray-700">{formatCurrency(summary?.totalValue)}</span></p>
          </div>
        </div>

        <div className="bg-white p-5 rounded-xl border border-gray-200 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-gray-500 uppercase">Đang hiệu lực</span>
            <span className="p-2 bg-emerald-50 text-emerald-600 rounded-lg text-lg">✅</span>
          </div>
          <div className="mt-3">
            <span className="text-2xl font-bold text-emerald-600">{summary?.activeContractsCount ?? 0}</span>
            <p className="text-xs text-gray-500 mt-1">Giá trị: <span className="font-semibold text-gray-700">{formatCurrency(summary?.activeContractsValue)}</span></p>
          </div>
        </div>

        <div className="bg-white p-5 rounded-xl border border-gray-200 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-gray-500 uppercase">Chờ phê duyệt</span>
            <span className="p-2 bg-amber-50 text-amber-600 rounded-lg text-lg">⏳</span>
          </div>
          <div className="mt-3">
            <span className="text-2xl font-bold text-amber-600">{summary?.pendingApprovalCount ?? 0}</span>
            <p className="text-xs text-gray-500 mt-1">Giá trị: <span className="font-semibold text-gray-700">{formatCurrency(summary?.pendingApprovalValue)}</span></p>
          </div>
        </div>

        <div className="bg-white p-5 rounded-xl border border-gray-200 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-gray-500 uppercase">Sắp hết hạn</span>
            <span className="p-2 bg-rose-50 text-rose-600 rounded-lg text-lg">⚠️</span>
          </div>
          <div className="mt-3">
            <span className="text-2xl font-bold text-rose-600">{summary?.expiringContractsCount ?? 0}</span>
            <p className="text-xs text-gray-500 mt-1">Giá trị: <span className="font-semibold text-gray-700">{formatCurrency(summary?.expiringContractsValue)}</span></p>
          </div>
        </div>
      </div>

      {/* Charts Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Status Distribution Pie Chart */}
        <div className="bg-white p-5 rounded-xl border border-gray-200 shadow-sm flex flex-col">
          <h2 className="text-sm font-bold text-gray-800 uppercase tracking-wide mb-4">
            Phân bố hợp đồng theo trạng thái
          </h2>
          <div className="h-64 w-full">
            {pieChartData.length === 0 ? (
              <div className="flex items-center justify-center h-full text-gray-400 text-sm">
                Chưa có dữ liệu trạng thái
              </div>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={pieChartData}
                    dataKey="value"
                    nameKey="name"
                    cx="50%"
                    cy="50%"
                    outerRadius={80}
                    label={({ name, percent }: { name?: string; percent?: number }) =>
                      `${name ?? ''}: ${((percent ?? 0) * 100).toFixed(0)}%`
                    }
                  >
                    {pieChartData.map((_, index) => (
                      <Cell key={`cell-${index}`} fill={STATUS_COLORS[index % STATUS_COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip
                    formatter={(val: unknown) => {
                      const num = typeof val === 'number' ? val : Number(val) || 0;
                      return [`${num} hợp đồng`, 'Số lượng'];
                    }}
                  />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>

        {/* Monthly Trend Chart */}
        <div className="bg-white p-5 rounded-xl border border-gray-200 shadow-sm flex flex-col">
          <h2 className="text-sm font-bold text-gray-800 uppercase tracking-wide mb-4">
            Xu hướng tạo hợp đồng theo tháng
          </h2>
          <div className="h-64 w-full">
            {trendChartData.length === 0 ? (
              <div className="flex items-center justify-center h-full text-gray-400 text-sm">
                Chưa có dữ liệu xu hướng
              </div>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={trendChartData}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} />
                  <XAxis dataKey="label" fontSize={12} tickLine={false} />
                  <YAxis fontSize={12} tickLine={false} allowDecimals={false} />
                  <Tooltip
                    formatter={(val: unknown) => {
                      const num = typeof val === 'number' ? val : Number(val) || 0;
                      return [`${num} hợp đồng`, 'Số lượng tạo'];
                    }}
                  />
                  <Line
                    type="monotone"
                    dataKey="count"
                    name="Số lượng"
                    stroke="#3b82f6"
                    strokeWidth={3}
                    dot={{ r: 4 }}
                    activeDot={{ r: 6 }}
                  />
                </LineChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>
      </div>

      {/* Department and Partner breakdown */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Department Bar Chart */}
        <div className="bg-white p-5 rounded-xl border border-gray-200 shadow-sm">
          <h2 className="text-sm font-bold text-gray-800 uppercase tracking-wide mb-4">
            Hợp đồng theo phòng ban
          </h2>
          <div className="h-64 w-full">
            {deptData.length === 0 ? (
              <div className="flex items-center justify-center h-full text-gray-400 text-sm">
                Chưa có dữ liệu phòng ban
              </div>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={deptData} layout="vertical" margin={{ left: 20 }}>
                  <CartesianGrid strokeDasharray="3 3" horizontal={false} />
                  <XAxis type="number" fontSize={12} allowDecimals={false} />
                  <YAxis
                    dataKey="departmentName"
                    type="category"
                    fontSize={12}
                    width={100}
                    tickLine={false}
                  />
                  <Tooltip
                    formatter={(val: unknown) => {
                      const num = typeof val === 'number' ? val : Number(val) || 0;
                      return [`${num} hợp đồng`, 'Số lượng'];
                    }}
                  />
                  <Bar dataKey="count" name="Số hợp đồng" fill="#6366f1" radius={[0, 4, 4, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>

        {/* Top Partners Table */}
        <div className="bg-white p-5 rounded-xl border border-gray-200 shadow-sm flex flex-col">
          <h2 className="text-sm font-bold text-gray-800 uppercase tracking-wide mb-4">
            Top đối tác hàng đầu
          </h2>
          <div className="overflow-x-auto flex-1">
            {partnerData.length === 0 ? (
              <div className="flex items-center justify-center h-full text-gray-400 text-sm py-12">
                Chưa có dữ liệu đối tác
              </div>
            ) : (
              <table className="w-full text-left text-xs">
                <thead>
                  <tr className="border-b border-gray-200 text-gray-500 uppercase">
                    <th className="py-2.5 px-3 font-semibold">Đối tác</th>
                    <th className="py-2.5 px-3 font-semibold text-center">Số HĐ</th>
                    <th className="py-2.5 px-3 font-semibold text-right">Tổng giá trị</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {partnerData.map((p) => (
                    <tr key={p.partnerId} className="hover:bg-gray-50">
                      <td className="py-2.5 px-3 font-medium text-gray-900">{p.partnerName}</td>
                      <td className="py-2.5 px-3 text-center text-gray-600 font-semibold">{p.count}</td>
                      <td className="py-2.5 px-3 text-right font-medium text-gray-800">
                        {formatCurrency(p.totalValue)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
