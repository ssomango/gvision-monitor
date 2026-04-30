class GvisionEvent {
  final int id;
  final String time;
  final String? package;
  final int? lotId;
  final int? camera;
  final int? inspection;
  final int logType;
  final String description;
  final String? imagePath;

  const GvisionEvent({
    required this.id,
    required this.time,
    this.package,
    this.lotId,
    this.camera,
    this.inspection,
    required this.logType,
    required this.description,
    this.imagePath,
  });

  factory GvisionEvent.fromJson(Map<String, dynamic> json) => GvisionEvent(
        id: json['Id'] as int? ?? json['id'] as int? ?? 0,
        time: json['Time'] as String? ?? json['time'] as String? ?? '',
        package: json['Package'] as String?,
        lotId: json['LotId'] as int?,
        camera: json['Camera'] as int?,
        inspection: json['Inspection'] as int?,
        logType: json['LogType'] as int? ?? json['logType'] as int? ?? 0,
        description: json['Description'] as String? ??
            json['description'] as String? ??
            '',
        imagePath: json['ImagePath'] as String?,
      );

  // ELog enum: SystemLogs=1, InspectionLogs=2, DatabaseLogs=3, LOTLogs=4, RecipeLogs=5
  bool get isAlert => logType == 1 || logType == 2;

  String get logTypeLabel {
    switch (logType) {
      case 1:
        return '시스템';
      case 2:
        return '검사';
      case 3:
        return 'DB';
      case 4:
        return 'LOT';
      case 5:
        return 'Recipe';
      default:
        return '기타';
    }
  }
}
