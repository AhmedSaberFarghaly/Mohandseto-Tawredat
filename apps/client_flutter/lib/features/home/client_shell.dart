import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme/app_tokens.dart';

class ClientShell extends StatelessWidget {
  const ClientShell({super.key, required this.child});
  final Widget child;

  static const routes = [
    '/home',
    '/categories',
    '/rfqs',
    '/orders',
    '/account',
  ];

  @override
  Widget build(BuildContext context) {
    final location = GoRouterState.of(context).uri.path;
    var index = routes.indexWhere((route) => location.startsWith(route));
    if (index < 0) index = 0;
    return Scaffold(
      backgroundColor: Colors.white,
      body: child,
      bottomNavigationBar: _ReferenceNavBar(
        index: index,
        onChanged: (value) => context.go(routes[value]),
      ),
    );
  }
}

class _NavItem {
  const _NavItem(this.icon, this.activeIcon, this.label);
  final IconData icon;
  final IconData activeIcon;
  final String label;
}

const _items = [
  _NavItem(Icons.home_outlined, Icons.home_rounded, 'الرئيسية'),
  _NavItem(Icons.grid_view_outlined, Icons.grid_view_rounded, 'الأقسام'),
  _NavItem(Icons.add_rounded, Icons.add_rounded, 'طلب عرض سعر'),
  _NavItem(Icons.receipt_long_outlined, Icons.receipt_long_rounded, 'طلباتي'),
  _NavItem(Icons.person_outline_rounded, Icons.person_rounded, 'حسابي'),
];

class _ReferenceNavBar extends StatelessWidget {
  const _ReferenceNavBar({required this.index, required this.onChanged});
  final int index;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) => SafeArea(
    minimum: const EdgeInsets.fromLTRB(10, 0, 10, 8),
    child: SizedBox(
      height: 92,
      child: Stack(
        clipBehavior: Clip.none,
        alignment: Alignment.bottomCenter,
        children: [
          Positioned(
            left: 0,
            right: 0,
            bottom: 0,
            child: Container(
              height: 76,
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(25),
                border: Border.all(color: AppColors.gray150),
                boxShadow: const [
                  BoxShadow(
                    color: Color(0x16102846),
                    blurRadius: 28,
                    offset: Offset(0, 8),
                  ),
                ],
              ),
            ),
          ),
          Positioned.fill(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                for (var i = 0; i < _items.length; i++)
                  Expanded(
                    child: i == 2
                        ? _QuoteNavButton(
                            active: index == i,
                            onTap: () => onChanged(i),
                          )
                        : _NavButton(
                            item: _items[i],
                            active: index == i,
                            onTap: () => onChanged(i),
                          ),
                  ),
              ],
            ),
          ),
        ],
      ),
    ),
  );
}

class _NavButton extends StatelessWidget {
  const _NavButton({
    required this.item,
    required this.active,
    required this.onTap,
  });
  final _NavItem item;
  final bool active;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(20),
    child: SizedBox(
      height: 76,
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          AnimatedSwitcher(
            duration: const Duration(milliseconds: 180),
            child: Icon(
              active ? item.activeIcon : item.icon,
              key: ValueKey(active),
              size: 25,
              color: active ? AppColors.primary : AppColors.gray500,
            ),
          ),
          const SizedBox(height: 5),
          Text(
            item.label,
            maxLines: 1,
            overflow: TextOverflow.fade,
            softWrap: false,
            style: TextStyle(
              color: active ? AppColors.primary : AppColors.gray500,
              fontSize: 10.5,
              fontWeight: active ? FontWeight.w700 : FontWeight.w600,
            ),
          ),
        ],
      ),
    ),
  );
}

class _QuoteNavButton extends StatelessWidget {
  const _QuoteNavButton({required this.active, required this.onTap});
  final bool active;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    customBorder: const CircleBorder(),
    child: SizedBox(
      height: 92,
      child: Column(
        mainAxisAlignment: MainAxisAlignment.start,
        children: [
          AnimatedContainer(
            duration: const Duration(milliseconds: 180),
            width: 58,
            height: 58,
            decoration: BoxDecoration(
              color: AppColors.primary,
              shape: BoxShape.circle,
              border: Border.all(color: Colors.white, width: 4),
              boxShadow: const [
                BoxShadow(
                  color: Color(0x40023BAA),
                  blurRadius: 18,
                  offset: Offset(0, 7),
                ),
              ],
            ),
            child: const Icon(Icons.add_rounded, color: Colors.white, size: 34),
          ),
          const SizedBox(height: 5),
          Text(
            'طلب عرض سعر',
            maxLines: 1,
            overflow: TextOverflow.fade,
            softWrap: false,
            style: TextStyle(
              color: active ? AppColors.primary : AppColors.gray600,
              fontSize: 9.5,
              fontWeight: active ? FontWeight.w700 : FontWeight.w600,
            ),
          ),
        ],
      ),
    ),
  );
}
