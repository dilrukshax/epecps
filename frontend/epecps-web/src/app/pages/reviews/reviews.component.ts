import { Component } from '@angular/core';

@Component({
  selector: 'app-reviews',
  templateUrl: './reviews.component.html',
  styleUrls: ['./reviews.component.css'],
  standalone: false
})
export class ReviewsComponent {
  pendingReviews = [
    { id: 1, reviewee: 'Alice Cooper', type: 'Peer Review', dueDate: '2024-12-20' },
    { id: 2, reviewee: 'Bob Wilson', type: 'Team Lead Review', dueDate: '2024-12-22' }
  ];

  completedReviews = [
    { id: 3, reviewee: 'Charlie Brown', type: 'Peer Review', completedDate: '2024-11-15' },
    { id: 4, reviewee: 'Diana Prince', type: 'Self Review', completedDate: '2024-11-10' }
  ];
}
