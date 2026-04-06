import { Component, OnInit } from '@angular/core';
import { EvaluationService } from '../../../services/evaluation.service';
import {
  EvaluationReportFilterDto,
  EvaluationReportDataDto,
  CycleDto,
  DepartmentDto
} from '../../../models/evaluation.models';

@Component({
  selector: 'app-hr-reports',
  templateUrl: './hr-reports.component.html',
  styleUrls: ['./hr-reports.component.css'],
  standalone: false
})
export class HrReportsComponent implements OnInit {
  loading = false;
  downloading = false;
  error: string | null = null;

  // Filter options
  cycles: CycleDto[] = [];
  departments: DepartmentDto[] = [];
  
  // Filter form
  filter: EvaluationReportFilterDto = {};
  
  // Report data
  reportData: EvaluationReportDataDto[] = [];
  filteredData: EvaluationReportDataDto[] = [];
  
  // Statistics
  stats = {
    totalEvaluations: 0,
    completed: 0,
    promoted: 0,
    averageScore: 0
  };

  // Debug info
  dbStats: any = null;
  showDebugInfo = false;

  constructor(private evaluationService: EvaluationService) {}

  ngOnInit(): void {
    this.loadFilterOptions();
    this.loadDatabaseStats();
  }

  loadFilterOptions(): void {
    this.loading = true;
    
    this.evaluationService.getCycles().subscribe({
      next: (cycles) => {
        this.cycles = cycles;
        console.log('Loaded cycles:', cycles);
      },
      error: (err) => {
        console.error('Error loading cycles:', err);
      }
    });

    this.evaluationService.getDepartments().subscribe({
      next: (departments) => {
        this.departments = departments;
        this.loading = false;
        console.log('Loaded departments:', departments);
      },
      error: (err) => {
        console.error('Error loading departments:', err);
        this.loading = false;
      }
    });
  }

  loadDatabaseStats(): void {
    // Call the new stats endpoint
    this.evaluationService.getDatabaseStats().subscribe({
      next: (stats) => {
        this.dbStats = stats;
        console.log('Database stats:', stats);
      },
      error: (err) => {
        console.error('Error loading database stats:', err);
      }
    });
  }

  applyFilters(): void {
    this.loading = true;
    this.error = null;

    console.log('Applying filters:', this.filter);

    this.evaluationService.getEvaluationReportData(this.filter).subscribe({
      next: (data) => {
        console.log('Received report data:', data);
        this.reportData = data;
        this.filteredData = data;
        this.calculateStatistics();
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error?.error || 'Failed to load report data. Please try again.';
        console.error('Error loading report data:', err);
        this.loading = false;
      }
    });
  }

  downloadReport(): void {
    this.downloading = true;
    this.error = null;

    this.evaluationService.downloadEvaluationReport(this.filter).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Evaluation_Report_${new Date().toISOString().split('T')[0]}.xlsx`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.downloading = false;
      },
      error: (err) => {
        this.error = err.error?.error || 'Failed to download report. Please try again.';
        this.downloading = false;
        console.error('Error downloading report:', err);
      }
    });
  }

  clearFilters(): void {
    this.filter = {};
    this.reportData = [];
    this.filteredData = [];
    this.stats = {
      totalEvaluations: 0,
      completed: 0,
      promoted: 0,
      averageScore: 0
    };
  }

  toggleDebugInfo(): void {
    this.showDebugInfo = !this.showDebugInfo;
    if (this.showDebugInfo && !this.dbStats) {
      this.loadDatabaseStats();
    }
  }

  calculateStatistics(): void {
    const completedStatuses = ['completed', 'completed_without_promotion', 'completed_with_promotion'];
    
    this.stats.totalEvaluations = this.filteredData.length;
    this.stats.completed = this.filteredData.filter(d => 
      completedStatuses.includes(d.status.toLowerCase())
    ).length;
    this.stats.promoted = this.filteredData.filter(d => d.isPromoted).length;
    
    const scoresAvailable = this.filteredData.filter(d => d.overallScore !== null && d.overallScore !== undefined);
    this.stats.averageScore = scoresAvailable.length > 0
      ? scoresAvailable.reduce((sum, d) => sum + (d.overallScore || 0), 0) / scoresAvailable.length
      : 0;
  }

  getStatusBadgeClass(status: string): string {
    const statusLower = status.toLowerCase();
    if (statusLower.includes('completed')) return 'bg-green-100 text-green-800';
    if (statusLower.includes('pending')) return 'bg-yellow-100 text-yellow-800';
    if (statusLower.includes('rejected')) return 'bg-red-100 text-red-800';
    return 'bg-blue-100 text-blue-800';
  }

  getStatusLabel(status: string): string {
    return status.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  getScoreColor(score: number | null | undefined): string {
    if (score === null || score === undefined) return 'text-gray-400';
    if (score >= 85) return 'text-green-600 font-bold';
    if (score >= 60) return 'text-yellow-600';
    return 'text-red-600';
  }

  formatDate(dateString: string | null | undefined): string {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString();
  }
}
