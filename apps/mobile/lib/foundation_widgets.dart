import 'package:flutter/material.dart';

class LfFoundationButton extends StatelessWidget {
  const LfFoundationButton({required this.label, required this.onPressed, this.secondary = false, super.key});
  final String label;
  final VoidCallback? onPressed;
  final bool secondary;

  @override
  Widget build(BuildContext context) => secondary
      ? OutlinedButton(onPressed: onPressed, child: Text(label))
      : FilledButton(onPressed: onPressed, child: Text(label));
}

class LfFoundationField extends StatelessWidget {
  const LfFoundationField({this.label, this.hint, this.controller, this.onChanged, super.key});
  final String? label;
  final String? hint;
  final TextEditingController? controller;
  final ValueChanged<String>? onChanged;

  @override
  Widget build(BuildContext context) => TextField(
        controller: controller,
        onChanged: onChanged,
        decoration: InputDecoration(labelText: label, hintText: hint),
      );
}

class LfFoundationCard extends StatelessWidget {
  const LfFoundationCard({required this.child, this.title, super.key});
  final String? title;
  final Widget child;

  @override
  Widget build(BuildContext context) => Card(child: Padding(padding: const EdgeInsets.all(16), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [if (title != null) ...[Text(title!, style: Theme.of(context).textTheme.titleMedium), const SizedBox(height: 12)], child])));
}

class LfLoadingState extends StatelessWidget {
  const LfLoadingState({super.key});
  @override
  Widget build(BuildContext context) => const Center(child: CircularProgressIndicator());
}

class LfEmptyState extends StatelessWidget {
  const LfEmptyState({required this.title, required this.message, super.key});
  final String title;
  final String message;
  @override
  Widget build(BuildContext context) => Column(children: [Text(title, style: const TextStyle(fontWeight: FontWeight.bold)), const SizedBox(height: 6), Text(message)]);
}

class LfErrorState extends StatelessWidget {
  const LfErrorState({required this.message, super.key});
  final String message;
  @override
  Widget build(BuildContext context) => Text(message, style: TextStyle(color: Theme.of(context).colorScheme.error));
}

Future<T?> showLfFoundationDialog<T>({required BuildContext context, required String title, required Widget child}) {
  return showDialog<T>(context: context, builder: (context) => AlertDialog(title: Text(title), content: child));
}

Future<T?> showLfFoundationBottomSheet<T>({required BuildContext context, required Widget child}) {
  return showModalBottomSheet<T>(context: context, builder: (context) => SafeArea(child: child));
}
