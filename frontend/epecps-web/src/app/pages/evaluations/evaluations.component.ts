import { Component } from '@angular/core';

@Component({
  selector: 'app-evaluations',
  templateUrl: './evaluations.component.html',
  styleUrls: ['./evaluations.component.css'],
  standalone: false
})
export class EvaluationsComponent {
  evaluations = [
    { id: 1, employee: 'John Doe', cycle: 'Q4 2024', status: 'In Progress', dueDate: '2024-12-31' },
    { id: 2, employee: 'Jane Smith', cycle: 'Q4 2024', status: 'Completed', dueDate: '2024-12-15' },
    { id: 3, employee: 'Mike Johnson', cycle: 'Q4 2024', status: 'Pending', dueDate: '2024-12-30' }
  ];
}
