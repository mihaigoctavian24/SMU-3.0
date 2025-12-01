# SMU Chart Components - Usage Examples

## 1. SmuLineChart

**Purpose**: Display grade evolution, performance trends over time

**Usage Example**:
```razor
@using SMU.Components.Shared.Charts

<SmuLineChart
    Title="Grade Evolution - Mathematics"
    Data="gradeData"
    YAxisLabel="Grade (1-10)"
    Height="400"
    IsLoading="isLoading" />

@code {
    private bool isLoading = false;
    private List<SmuLineChart.ChartDataPoint> gradeData = new()
    {
        new() { Date = new DateTime(2024, 9, 15), Value = 7.5m },
        new() { Date = new DateTime(2024, 10, 15), Value = 8.0m },
        new() { Date = new DateTime(2024, 11, 15), Value = 8.5m },
        new() { Date = new DateTime(2024, 12, 15), Value = 9.0m }
    };
}
```

---

## 2. SmuBarChart

**Purpose**: Compare faculties, courses, student counts

**Usage Example**:
```razor
@using SMU.Components.Shared.Charts

<SmuBarChart
    Title="Students per Faculty"
    Data="studentData"
    YAxisLabel="Number of Students"
    Height="400"
    Horizontal="false"
    IsLoading="isLoading" />

@code {
    private bool isLoading = false;
    private List<SmuBarChart.ChartDataItem> studentData = new()
    {
        new() { Label = "Computer Science", Value = 450 },
        new() { Label = "Engineering", Value = 380 },
        new() { Label = "Medicine", Value = 320 },
        new() { Label = "Law", Value = 280 }
    };
}
```

**Horizontal Mode**:
```razor
<SmuBarChart
    Title="Top 10 Courses by Enrollment"
    Data="courseData"
    YAxisLabel="Students"
    Horizontal="true"
    Height="500" />
```

---

## 3. SmuDonutChart

**Purpose**: Grade distribution, attendance types, enrollment status

**Usage Example**:
```razor
@using SMU.Components.Shared.Charts

<SmuDonutChart
    Title="Grade Distribution"
    Data="gradeDistribution"
    Height="350"
    ShowLegend="true"
    IsLoading="isLoading" />

@code {
    private bool isLoading = false;
    private List<SmuDonutChart.ChartDataSegment> gradeDistribution = new()
    {
        new() { Label = "10 (Excellent)", Value = 15, Color = "#10B981" },
        new() { Label = "9-8 (Good)", Value = 45, Color = "#4F46E5" },
        new() { Label = "7-6 (Fair)", Value = 30, Color = "#F59E0B" },
        new() { Label = "5 (Pass)", Value = 8, Color = "#EF4444" },
        new() { Label = "< 5 (Fail)", Value = 2, Color = "#7F1D1D" }
    };
}
```

**With Auto Colors** (if Color not specified):
```razor
private List<SmuDonutChart.ChartDataSegment> attendanceTypes = new()
{
    new() { Label = "Present", Value = 850 },
    new() { Label = "Absent", Value = 120 },
    new() { Label = "Excused", Value = 30 }
};
```

---

## 4. SmuRadarChart

**Purpose**: Multi-dimensional performance (student across courses, faculty metrics)

**Usage Example**:
```razor
@using SMU.Components.Shared.Charts

<SmuRadarChart
    Title="Student Performance Profile"
    Data="performanceData"
    Height="400"
    IsLoading="isLoading" />

@code {
    private bool isLoading = false;
    private List<SmuRadarChart.ChartDataCategory> performanceData = new()
    {
        new() { Category = "Mathematics", Value = 8.5m },
        new() { Category = "Physics", Value = 7.8m },
        new() { Category = "Programming", Value = 9.2m },
        new() { Category = "English", Value = 8.0m },
        new() { Category = "Chemistry", Value = 7.5m },
        new() { Category = "History", Value = 6.8m }
    };
}
```

**Faculty Comparison**:
```razor
private List<SmuRadarChart.ChartDataCategory> facultyMetrics = new()
{
    new() { Category = "Student Satisfaction", Value = 8.5m },
    new() { Category = "Employment Rate", Value = 9.0m },
    new() { Category = "Research Output", Value = 7.5m },
    new() { Category = "Funding", Value = 6.8m },
    new() { Category = "Facilities", Value = 8.2m }
};
```

---

## 5. SmuHeatmapChart

**Purpose**: Attendance calendar visualization, monthly patterns

**Usage Example**:
```razor
@using SMU.Components.Shared.Charts

<SmuHeatmapChart
    Title="November 2024 Attendance"
    Data="attendanceData"
    Month="11"
    Year="2024"
    Height="400"
    IsLoading="isLoading" />

@code {
    private bool isLoading = false;
    private Dictionary<DateTime, decimal> attendanceData = new()
    {
        { new DateTime(2024, 11, 1), 95 },
        { new DateTime(2024, 11, 2), 88 },
        { new DateTime(2024, 11, 3), 92 },
        { new DateTime(2024, 11, 4), 78 },
        { new DateTime(2024, 11, 5), 85 },
        { new DateTime(2024, 11, 8), 90 },
        { new DateTime(2024, 11, 9), 87 },
        { new DateTime(2024, 11, 10), 93 },
        // ... more days
    };
}
```

**Color Coding**:
- 0-30%: Red (Poor)
- 31-60%: Amber (Fair)
- 61-85%: Yellow (Good)
- 86-100%: Green (Excellent)

---

## Common Features

### Loading State
All charts support `IsLoading` parameter:
```razor
<SmuLineChart IsLoading="true" ... />
```

### Empty State
All charts display friendly empty state when `Data` is null or empty.

### Responsive Design
All charts automatically adjust for mobile devices (< 768px breakpoint).

### Download Feature
All charts include built-in download button in toolbar (PNG format).

---

## Integration in Dashboard

**Complete Example**:
```razor
@page "/dashboard/analytics"
@using SMU.Components.Shared.Charts

<div class="p-6 space-y-6">
    <h1 class="text-2xl font-bold text-gray-900">Analytics Dashboard</h1>

    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Grade Trends -->
        <SmuLineChart
            Title="Average Grade Trends"
            Data="gradeTrends"
            YAxisLabel="Average Grade"
            Height="350" />

        <!-- Faculty Comparison -->
        <SmuBarChart
            Title="Students per Faculty"
            Data="facultyData"
            YAxisLabel="Students"
            Height="350" />

        <!-- Grade Distribution -->
        <SmuDonutChart
            Title="Overall Grade Distribution"
            Data="gradeDistribution"
            Height="350"
            ShowLegend="true" />

        <!-- Student Performance -->
        <SmuRadarChart
            Title="Average Performance by Subject"
            Data="subjectPerformance"
            Height="350" />
    </div>

    <!-- Attendance Heatmap (Full Width) -->
    <div class="w-full">
        <SmuHeatmapChart
            Title="December 2024 Attendance Overview"
            Data="attendanceCalendar"
            Month="12"
            Year="2024"
            Height="400" />
    </div>
</div>

@code {
    // Data initialization here...
}
```

---

## Notes

- All charts use Tailwind CSS color scheme (indigo, emerald, amber, red)
- Charts are fully responsive with mobile breakpoints
- Empty states and loading states built-in
- Download functionality enabled by default
- Smooth animations and transitions
- Tooltips show formatted values on hover
