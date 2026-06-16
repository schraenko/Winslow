// lib/features/requirements/domain/requirement.dart

enum RequirementStatus {
  draft,
  underReview,
  approved,
  rejected,
  implemented;

  String get label => switch (this) {
        RequirementStatus.draft        => 'Entwurf',
        RequirementStatus.underReview  => 'In Prüfung',
        RequirementStatus.approved     => 'Freigegeben',
        RequirementStatus.rejected     => 'Abgelehnt',
        RequirementStatus.implemented  => 'Umgesetzt',
      };

  static RequirementStatus fromString(String s) => switch (s.toLowerCase()) {
        'draft'        => RequirementStatus.draft,
        'underreview'  => RequirementStatus.underReview,
        'approved'     => RequirementStatus.approved,
        'rejected'     => RequirementStatus.rejected,
        'implemented'  => RequirementStatus.implemented,
        _              => throw ArgumentError('Unbekannter Status: $s'),
      };
}

enum RequirementPriority {
  mustHave,
  shouldHave,
  couldHave,
  wontHave;

  String get label => switch (this) {
        RequirementPriority.mustHave   => 'Must Have',
        RequirementPriority.shouldHave => 'Should Have',
        RequirementPriority.couldHave  => 'Could Have',
        RequirementPriority.wontHave   => "Won't Have",
      };
}

enum RequirementKind {
  functional,
  nonFunctional,
  constraint,
  businessRule;

  String get label => switch (this) {
        RequirementKind.functional    => 'Funktional',
        RequirementKind.nonFunctional => 'Nicht-funktional',
        RequirementKind.constraint    => 'Randbedingung',
        RequirementKind.businessRule  => 'Geschäftsregel',
      };
}

class Requirement {
  const Requirement({
    required this.id,
    required this.projectId,
    required this.title,
    required this.description,
    required this.status,
    required this.priority,
    required this.kind,
    required this.acceptanceCriteria,
    required this.authorId,
    required this.createdAt,
    required this.updatedAt,
  });

  final String id;
  final String projectId;
  final String title;
  final String description;
  final RequirementStatus status;
  final RequirementPriority priority;
  final RequirementKind kind;
  final List<String> acceptanceCriteria;
  final String authorId;
  final DateTime createdAt;
  final DateTime updatedAt;

  factory Requirement.fromJson(Map<String, dynamic> json) => Requirement(
        id                 : json['id'] as String,
        projectId          : json['projectId'] as String,
        title              : json['title'] as String,
        description        : json['description'] as String,
        status             : RequirementStatus.fromString(json['status'] as String),
        priority           : RequirementPriority.values.firstWhere(
                               (p) => p.name.toLowerCase() == (json['priority'] as String).toLowerCase()),
        kind               : RequirementKind.values.firstWhere(
                               (k) => k.name.toLowerCase() == (json['kind'] as String).toLowerCase()),
        acceptanceCriteria : List<String>.from(json['acceptanceCriteria'] as List),
        authorId           : json['authorId'] as String,
        createdAt          : DateTime.parse(json['createdAt'] as String),
        updatedAt          : DateTime.parse(json['updatedAt'] as String),
      );

  Requirement copyWith({RequirementStatus? status}) => Requirement(
        id                 : id,
        projectId          : projectId,
        title              : title,
        description        : description,
        status             : status ?? this.status,
        priority           : priority,
        kind               : kind,
        acceptanceCriteria : acceptanceCriteria,
        authorId           : authorId,
        createdAt          : createdAt,
        updatedAt          : updatedAt,
      );
}
